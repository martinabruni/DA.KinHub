# Descrizione generale

La macro feature **Autenticazione e Identità** gestisce l'intero ciclo di vita dell'account utente di KinHub e l'emissione dei token con cui tutte le altre API autorizzano le richieste. È il "guardiano" del sistema: nessun'altra feature funziona senza un token valido prodotto qui.

Cosa fa, in concreto:

- Registrazione di un nuovo utente con email + password.
- Login tramite un **server OAuth 2.0 custom** (grant *Authorization Code* con **PKCE S256**), che restituisce alle SPA un access token JWT con scope.
- Emissione e **refresh** dei token; logout con revoca del refresh token.
- Gestione del profilo (`GET/PUT /api/auth/me`, cambio email e password, cancellazione account).
- Collegamento/scollegamento di **provider di identità** (oggi solo il provider password "KinHub").
- Esposizione dell'endpoint `GET /api/access/family-context` usato dagli altri servizi per sapere a quale famiglia appartiene l'utente.

Responsabilità e confini: questa feature vive interamente nel contesto **Identity** ed è esposta dall'host `Kin.KinHub.Identity.Api`. È l'unica che conosce le credenziali e che firma i JWT.

Parti del backend coinvolte:

- **Presentation** — `AuthController`, `OAuthController`, `OAuthMetadataController`, `AccessController` (`src/Presentations/Kin.KinHub.Identity.Api`), più validator FluentValidation e gli store OAuth (`PostgreSqlOAuthStores`, `OAuthModels`). I servizi helper OAuth (`OAuthRequestValidator`, `OAuthSessionManager`, `OAuthTokenIssuer`, `OAuthLoginPageRenderer`) vivono in `AuthenticationFeature/Services`.
- **Business** — `KinHubAuthenticationService` e gli handler in `AuthenticationFeature/Commands` e `Queries` (`Register`, `Login`, `Refresh`, `Logout`, `UpdateUserEmail`, `UpdateUserPassword`, `DeleteUser`, `GetCurrentUser`), più `KinHubPasswordIdentityProvider`, `IdentityProviderRegistry`, `LoginResponseFactory`, `UserProviderService`.
- **Domain** — entità `KinUser`, `UserCredential`, `UserProvider`, `Provider`, `RefreshToken`, `TokenClaims`; interfacce `IIdentityProvider`, `IIdentityProviderRegistry`, `ITokenGenerator`, `ITokenValidator`, `IPasswordHasher`, i repository, l'enum `UserStatus`, `IdentityProviderType`.
- **Infrastructure** — `Kin.KinHub.Identity.Jwt` (`JwtTokenGenerator`, `CurrentUser`, `JwtOptions`), `Kin.KinHub.Identity.PostgreSql` (repository, `PasswordHasher`, `IdentityDbContext`).

Dati ricevuti: credenziali (email/password), refresh token, richieste OAuth (client_id, redirect_uri, scope, code_challenge). Dati prodotti: `RegisterResponse`, `LoginResponse` (access token + refresh token + scadenza + email/displayName), `UserProfileResponse`, e le risposte OAuth (`access_token`, `token_type`, `expires_in`, `scope`).

Dipendenze principali: `System.IdentityModel.Tokens.Jwt` per i JWT, EF Core/Npgsql per la persistenza, FluentValidation per la validazione, il rate limiter nativo per gli endpoint OAuth.

# Casi d'uso

- **Registrazione utente** — *Obiettivo*: creare un account. *Attore*: utente anonimo (SPA di registrazione). *Input*: `RegisterRequest` (email, displayName, password). *Output*: `RegisterResponse` (userId, email), HTTP 201. *Condizioni/errori*: email duplicata → 409 Conflict; password non valida (es. mancante) → `DomainValidationException` → 400 ValidationError.
- **Login (OAuth Authorization Code + PKCE)** — *Obiettivo*: ottenere un access token. *Attore*: SPA client registrata. *Input*: parametri OAuth su `/authorize` + email/password sul form. *Output*: redirect con `code`, poi `access_token` scambiato su `/token`. *Condizioni*: obbligatori PKCE `S256`, redirect_uri registrato, scope supportato; consenso esplicito per scope elevati (`write`/`admin`).
- **Refresh del token** — *Obiettivo*: rinnovare l'access token senza reinserire le credenziali. *Attore*: SPA (silent authorize sul cookie di sessione). *Input*: refresh token memorizzato nella sessione OAuth. *Output*: nuovo `LoginResponse`. *Condizioni/errori*: token inesistente/revocato/scaduto o account non attivo → 401.
- **Logout** — *Obiettivo*: terminare la sessione. *Input*: cookie di sessione (o refresh token). *Output*: revoca del refresh token + cancellazione cookie; redirect o 204.
- **Profilo utente** — *Obiettivo*: leggere/aggiornare i dati account. *Attore*: utente autenticato. *Input*: token; per gli aggiornamenti `UpdateUserEmailRequest`/`UpdateUserPasswordRequest`. *Output*: `UserProfileResponse` o booleano di esito.
- **Collega/scollega provider** — *Obiettivo*: gestire i metodi di accesso. *Input*: `LinkProviderRequest` o tipo provider da rimuovere. *Output*: lista provider collegati o esito. *Nota*: oggi è realmente implementato solo il provider `KinHub` (password); l'infrastruttura a registry è predisposta per altri provider ma non ne esistono altri nel codice → estensione **solo parzialmente sfruttata**.
- **Registrazione dinamica client OAuth** — *Obiettivo*: consentire la registrazione di client pubblici. *Condizione*: attiva solo se `EnableDynamicClientRegistration` è vera; sono ammessi solo client pubblici (`token_endpoint_auth_method = none`), grant `authorization_code`, redirect HTTPS/localhost.

# Flusso implementativo

## 1. Punto di ingresso

Due percorsi distinti:

- **Registrazione**: `POST /api/auth/register` → `AuthController.RegisterAsync` con body `RegisterRequest`.
- **Login/OAuth**: `GET /authorize` e `POST /authorize` → `OAuthController.Authorize/AuthorizeAsync`; scambio del codice su `POST /token` → `OAuthController.Token`. Il profilo passa da `AuthController` (`/api/auth/me`, `me/email`, `me/password`, `me/providers`).

Il bootstrap di tutto avviene in `Program.cs` → `AddKinHubIdentityApi` (`ServiceCollectionExtensions`), che configura JWT Bearer, le policy di autorizzazione, il rate limiter OAuth, CORS, gli store OAuth (in-memory in sviluppo, PostgreSQL in produzione) e registra `AddKinHubFamilyBusiness().AddKinHubIdentityBusiness()`.

## 2. Validazione iniziale

- I controller controllano prima il **null del body** (`ApiProblemDetails.InvalidRequestBody`) e poi eseguono la validazione FluentValidation via `IRequestValidator<T>` (es. `RegisterRequestValidator`, `UpdateUserPasswordRequestValidator`); in caso di errori → 400 con lista errori.
- L'`OAuthController` delega la validazione della richiesta di autorizzazione a `OAuthRequestValidator`: `response_type=code`, `client_id` conosciuto, `redirect_uri` registrato **e** ammesso (`IsAllowedRedirectUri`: solo HTTPS o localhost/127.0.0.1), `code_challenge` presente con `code_challenge_method=S256`, e scope normalizzato/consentito.
- Gli endpoint autenticati usano l'attributo `[Authorize]` (default policy) che richiede utente autenticato **e** scope `read`.

## 3. Orchestrazione applicativa

- `AuthController` delega a `IAuthenticationService` → `KinHubAuthenticationService`, che è una **facciata** che inoltra ad ogni handler dedicato (es. `RegisterAsync` → `RegisterUserHandler`).
- `RegisterUserHandler` risolve il provider dal registry: `_providerRegistry.Resolve(IdentityProviderType.KinHub)` e chiama `provider.RegisterAsync(IdentityRegistration)`.
- `LoginUserHandler` chiama `provider.AuthenticateAsync(IdentityCredential)`; se l'utente è valido delega a `ILoginResponseFactory.CreateAsync` per generare i token.
- `LoginResponseFactory` chiama `ITokenGenerator.GenerateAccessToken` + `GenerateRefreshToken`, persiste il `RefreshToken` (scadenza a 7 giorni) e compone `LoginResponse`.
- Nel flusso OAuth, `OAuthController` è un **thin orchestrator**: `AuthorizeAsync` delega a `OAuthRequestValidator` (validazione), `OAuthSessionManager` (gestione cookie di sessione e rehydration del login — `await _sessionManager.RehydrateLoginResponseAsync(...)` senza più sync-over-async), `OAuthTokenIssuer` (emissione/scambio codice e token) e `OAuthLoginPageRenderer` (rendering HTML della pagina di login). Il flusso `AuthorizeAsync` è interamente asincrono. **Non viene emesso refresh_token OAuth**: le SPA rinnovano con un *silent authorize* sul cookie di sessione.

## 4. Logica di dominio

- La regola centrale del provider password è in `KinHubPasswordIdentityProvider`: in `RegisterAsync` crea `KinUser` (stato `Active`, email non verificata), salva la `UserCredential` con hash della password e crea il link `UserProvider`. In `AuthenticateAsync` verifica che l'utente esista, sia `Active`, abbia una credenziale e che `IPasswordHasher.Verify` sia positivo; altrimenti ritorna `null` (login fallito).
- Invarianti applicate: password obbligatoria per registrare/collegare il provider (`DomainValidationException` altrimenti); refresh token utilizzabile una sola volta (in `RefreshTokenHandler` viene marcato `Revoked = true` prima di emetterne uno nuovo — rotazione dei refresh token); solo utenti `Active` possono rinnovare.
- `KinUser`, `RefreshToken`, ecc. estendono `BaseDeletableEntity<Guid>` (Id, timestamp di audit, soft delete): le entità hanno un minimo di comportamento ma la logica risiede nei provider/handler.

## 5. Accesso ai dati

- Repository in `Kin.KinHub.Identity.PostgreSql`: `KinUserRepository` (`FindByEmailAsync`, `CreateAsync`), `UserCredentialRepository` (`GetByUserIdAsync`), `UserProviderRepository`, `RefreshTokenRepository` (`FindByTokenAsync`, `UpdateAsync`), `ProviderRepository`. Persistenza su `IdentityDbContext` (EF Core + Npgsql).
- Scritture: creazione utente + credenziale + link provider in registrazione; creazione del refresh token in login; update (revoca) del refresh token in refresh; update di email/password nei rispettivi handler.
- Gli store OAuth per **authorization code** e **sessione identità** sono in-memory in sviluppo e su PostgreSQL in produzione (`PostgreSqlOAuthAuthorizationCodeStore`, `PostgreSqlOAuthIdentitySessionStore`); gli store dei client e degli scope dei refresh token sono in-memory basati su `OAuthServerOptions`.

## 6. Integrazioni esterne

- Nessun provider di identità esterno reale (no Google/Microsoft nel codice): l'unica integrazione "esterna" è concettuale (il registry pronto ad accoglierne). La firma dei token è locale (HMAC-SHA256 con segreto simmetrico).
- Azure Monitor/OpenTelemetry è attivabile per la telemetria se configurato.

## 7. Gestione errori

- Gli handler traducono le eccezioni di dominio in `Result<T>`: `DuplicateEntityException` → `Conflict`, `EntityNotFoundException` → `Unauthorized/NotFound` a seconda del caso, `DomainValidationException` → `ValidationError` (400), `DomainException` → `UnexpectedError`. Le password errate e i token invalidi diventano `Unauthorized` **senza** rivelare quale campo è sbagliato ("Invalid email or password.", "Invalid or expired refresh token.").
- `HttpResultMapper.ToActionResult(controller, result)` mappa `ResultStatus` → HTTP: Success 200/201, NotFound 404, Conflict 409, ValidationError 400, Unauthorized **403**, ServiceUnavailable 503, altro 500; con corpo `ProblemDetails` (RFC 9457) contenente `code`, `correlationId` e opzionalmente `errors`.
- Sugli errori JWT lato pipeline, `JwtBearerEvents.OnChallenge`/`OnForbidden` scrivono direttamente un problem detail 401/403 via `ApiProblemDetails.WriteAsync`.
- Gli errori OAuth seguono il formato standard `{ error, error_description }` (`CreateOAuthError`) e, dove appropriato, redirigono l'errore al `redirect_uri`. Il rate limiter risponde 429; gli store possono lanciare `InvalidOperationException` mappata a 429 "slow_down".

## 8. Output finale

- Registrazione: `KinUser` + `UserCredential` + `UserProvider` persistiti; risposta 201 con `RegisterResponse`.
- Login OAuth: `RefreshToken` persistito, sessione + cookie creati, authorization code memorizzato, e infine JSON `{ access_token, token_type: "Bearer", expires_in, scope }`.
- Refresh: vecchio refresh token revocato, nuovo refresh token persistito, nuovo access token emesso.
- Logout: refresh token revocato (`LogoutUserHandler`), cookie di sessione cancellato.
- Side effect trasversale: l'access token emesso è ciò che tutte le altre API validano; `AccessController.GetFamilyContext` (protetto dalla policy famiglia) restituisce `{ familyId }` agli altri servizi.

# Pattern correttamente implementati

- **Strategy + Registry (provider di identità)** — `IIdentityProvider` (Domain) con implementazione `KinHubPasswordIdentityProvider` e risoluzione via `IdentityProviderRegistry` (`Resolve(IdentityProviderType)`). *Perché è presente*: gli handler non conoscono l'implementazione, chiedono al registry per tipo. *Problema risolto*: rende estendibile l'autenticazione con nuovi provider senza toccare gli handler. *Correttezza*: il registry costruisce un dizionario per `ProviderType` da tutti gli `IIdentityProvider` iniettati; l'aggiunta di un provider è una sola registrazione DI. *Limite*: esiste un solo provider concreto, quindi il beneficio è potenziale.

- **Facade / Application Service** — `KinHubAuthenticationService` implementa `IAuthenticationService` inoltrando ad handler specifici. *Perché corretto*: unica superficie stabile per i controller, single responsibility per ciascun handler. *Limite*: è puro passthrough (nessuna orchestrazione aggiuntiva), quindi aggiunge un livello di indirezione.

- **Factory** — `LoginResponseFactory` centralizza la creazione coerente di access token + refresh token + persistenza. *Correttezza*: incapsula la scadenza (7 giorni) e la struttura di `LoginResponse` in un solo punto riusato da login e refresh.

- **Result Pattern** — `Result<T>`/`ResultStatus` separano l'esito applicativo dal trasporto HTTP; gli handler non lanciano eccezioni verso i controller. *Correttezza*: mapping centralizzato e coerente in `HttpResultMapper`; `DomainValidationException` è mappata correttamente a `ValidationError` (400).

- **Options Pattern con validazione fail-fast** — `JwtOptions`, `OAuthServerOptions`, `CorsOptions` letti dalla configurazione; `ValidateProductionSecurity` impedisce l'avvio in produzione con segreti deboli o redirect non-HTTPS. *Correttezza*: sposta gli errori di configurazione all'avvio anziché a runtime.

- **PKCE / OAuth Authorization Code** — `OAuthController` implementa `code_challenge_method=S256`, verifica `VerifyPkce` (SHA-256 + Base64Url), consuma il codice una sola volta (`TryConsume`) e vincola client/redirect. *Correttezza*: aderisce alle pratiche OAuth per client pubblici (nessun client secret, PKCE obbligatorio, consenso elevato esplicito per scope `write`/`admin`).

- **Decomposizione del controller OAuth** — `OAuthController` (~344 righe) è un thin orchestrator; la validazione OAuth è in `OAuthRequestValidator`, la gestione cookie/sessione in `OAuthSessionManager`, l'emissione dei token in `OAuthTokenIssuer`, il rendering HTML in `OAuthLoginPageRenderer`. *Correttezza*: ogni servizio ha una singola responsabilità, il controller non conosce i dettagli di implementazione.

# Anti-pattern

- **Facciata puramente passthrough** — *File*: `KinHubAuthenticationService.cs`. Ogni metodo inoltra 1:1 all'handler. *Problema*: livello di indirezione senza logica propria; duplica le firme. *Impatto*: leggibilità/boilerplate. *Gravità*: bassa. *Direzione*: accettabile come punto di composizione, ma valutabile un'esposizione diretta degli handler.

> Dove il comportamento dipende da configurazione di ambiente (es. store OAuth in-memory vs PostgreSQL, abilitazione della registrazione dinamica dei client) i dettagli runtime non sono deducibili con certezza dalla codebase analizzata.
