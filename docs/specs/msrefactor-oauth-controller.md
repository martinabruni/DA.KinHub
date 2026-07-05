> Stato validazione: PASS
> Iterazioni eseguite: 2

# Obiettivo del refactor

Ridurre il rischio e migliorare la manutenibilità del server OAuth 2.0 custom di KinHub, oggi concentrato in un unico `OAuthController` di grandi dimensioni. Due obiettivi distinti e prioritizzati:

1. **Correttezza/affidabilità (prioritario)**: eliminare la chiamata *sync-over-async* `RefreshTokenAsync(...).GetAwaiter().GetResult()` in `RehydrateLoginResponse`, che su un percorso di richiesta ASP.NET può causare thread-pool starvation/deadlock proprio sull'endpoint di autenticazione.
2. **Manutenibilità/sicurezza (secondario)**: separare le responsabilità del controller (validazione OAuth, gestione sessione/cookie, PKCE, ri-firma token, **rendering HTML della pagina di login**) in componenti dedicati, così che la logica di sicurezza sia isolabile e testabile.

Problema risolto: rischio di instabilità in produzione sull'autenticazione e difficoltà di manutenere/testare in sicurezza un controller "onnisciente".

# Stato attuale

`OAuthController` (`src/Presentations/Kin.KinHub.Identity.Api/AuthenticationFeature/Controllers/OAuthController.cs`, ~730 righe) implementa un Authorization Server con grant Authorization Code + PKCE (S256). Responsabilità concentrate nella stessa classe:

- **Endpoint**: `POST /register` (dynamic client registration), `GET /authorize`, `POST /authorize` (`AuthorizeAsync`), `POST /token` (`Token` → `ExchangeAuthorizationCode`), `POST /logout`.
- **Validazione OAuth**: `TryValidateAuthorizationRequest`, `TryNormalizeGrantedScope`, `TryResolveDynamicClientScope`, `IsAllowedRedirectUri` (solo HTTPS/localhost), `VerifyPkce` (SHA-256 + Base64Url).
- **Gestione sessione**: cookie `HttpOnly` (`WriteIdentitySessionCookie`, `DeleteIdentitySessionCookie`, `TryGetIdentitySession`), store `IOAuthIdentitySessionStore`/`IOAuthAuthorizationCodeStore` (in-memory in Development, PostgreSQL in produzione — vedi `PostgreSqlOAuthStores`).
- **Emissione token**: `CreateScopedTokenResponse` ri-valida l'access token e ne emette uno nuovo con gli scope concessi (nessun refresh_token OAuth: il rinnovo è un *silent authorize* sul cookie).
- **Rendering HTML**: `RenderLoginPage` costruisce a mano l'intera pagina HTML di login/consenso (centinaia di righe di markup con interpolazione di stringhe).
- **Punto critico**: `RehydrateLoginResponse` chiama `_authenticationService.RefreshTokenAsync(session.RefreshToken, HttpContext.RequestAborted).GetAwaiter().GetResult()` — un blocco sincrono su un metodo asincrono — invocato da `Authorize` (metodo `IActionResult` **sincrono**).

La configurazione (rate limiting, store, opzioni) è in `AddKinHubIdentityApi` (`ServiceCollectionExtensions`). Esistono già test rilevanti: `OAuthAndAccessIntegrationTests`, `OAuthGateTests`, `ProviderLinkUnlinkTests`, `AuthMeProvidersApiTests`.

# Problemi individuati

- **Sync-over-async su percorso di richiesta (bug reale / rischio di regressione e scalabilità)**: `GetAwaiter().GetResult()` in `RehydrateLoginResponse` blocca un thread del pool in attesa di un'operazione asincrona (che a sua volta fa I/O su DB per il refresh token). Sotto carico può causare **thread starvation e potenziali deadlock** proprio sul login silenzioso, con impatto di business diretto.
- **Fat Controller / violazione SRP (rischio architetturale + debito tecnico)**: un'unica classe gestisce validazione, sicurezza, sessione, emissione token e presentazione. Alta densità di logica di sicurezza difficile da isolare.
- **Rendering HTML nel controller (debito tecnico + rischio di sicurezza)**: `RenderLoginPage` genera markup con interpolazione di stringhe; pur usando `WebUtility.HtmlEncode` in più punti, la costruzione manuale di HTML mescola concern di presentazione con la sicurezza e aumenta la superficie per errori di encoding in future modifiche.
- **Testabilità ridotta (debito tecnico)**: la logica di scope/PKCE/sessione è annidata in metodi privati del controller, testabile solo via integrazione end-to-end.
- **Ri-firma del token con `KinUser` "sintetico" (rischio di regressione)**: `CreateScopedTokenResponse` ricostruisce un `KinUser` dai claim per riemettere il token; una logica delicata (scope/ruoli) incapsulata in un controller.

# Come Microsoft farebbe il refactor

Approccio incrementale in cui **la correzione del bug è separata e prioritaria** rispetto al refactor strutturale, per rilasciare valore/sicurezza subito con rischio minimo. Riferimenti concettuali: linee guida .NET sull'async ("async all the way", evitare `Task.Result`/`GetAwaiter().GetResult()` su percorsi di richiesta) e principi di separazione delle responsabilità.

1. **Prima il bug, poi il resto**: rendere asincrono il percorso `Authorize` così da chiamare `await RefreshTokenAsync(...)` senza blocco. È un cambiamento localizzato e a basso rischio, coperto dai test OAuth esistenti.
2. **Estrazione incrementale di service dedicati**, mantenendo il controller come *thin orchestrator*:
   - un `IOAuthRequestValidator` (validazione request/scope/redirect/PKCE);
   - un `IOAuthSessionManager` (cookie + store di sessione);
   - un `IOAuthTokenIssuer` (ri-firma token con scope);
   - un componente di **rendering** della pagina di login separato (view/Razor o template dedicato) invece di HTML inline.
3. **Interfacce solo dove servono**: estrarre i tre/quattro collaboratori reali; non creare astrazioni per ogni metodo.
4. **Sicurezza by design**: centralizzare l'encoding/rendering in un solo punto; test di sicurezza mirati su PKCE, validazione redirect e scope.
5. **Backward compatibility**: gli endpoint, i formati di risposta OAuth (`{ error, error_description }`, `{ access_token, token_type, expires_in, scope }`) e il comportamento restano identici; cambia solo l'organizzazione interna.
6. **Deploy progressivo + rollback**: ogni estrazione è un commit isolato; rollback = revert del singolo commit.

# Piano operativo

**Step 1 — Rete di sicurezza sui flussi OAuth.**
- *Cosa*: verificare/estendere `OAuthAndAccessIntegrationTests` e `OAuthGateTests` per coprire: authorize con sessione valida (silent), authorize senza sessione (pagina login), token exchange con PKCE valido/invalido, logout.
- *Dove*: `src/Tests/Kin.KinHub.Core.Test`.
- *Perché*: proteggere il comportamento prima di toccare il controller.
- *Impatto/Rischio*: nessuno sul runtime; basso.
- *Test dopo*: suite OAuth verde.

**Step 2 — Correggere il sync-over-async (priorità).**
- *Cosa*: convertire `Authorize` in `Task<IActionResult> AuthorizeAsync` (o estrarre il ramo con sessione in un metodo async) e sostituire `GetAwaiter().GetResult()` con `await`; rendere `RehydrateLoginResponse` asincrono.
- *Dove*: `OAuthController.cs` (`Authorize`, `RehydrateLoginResponse`).
- *Perché*: eliminare il rischio di deadlock/starvation.
- *Impatto previsto*: comportamento funzionale invariato; stabilità migliorata.
- *Rischio dello step*: medio (tocca il percorso di login); mitigato dai test Step 1.
- *Test dopo*: flusso silent authorize + refresh in integrazione.

**Step 3 — Estrarre il rendering della pagina di login.**
- *Cosa*: spostare `RenderLoginPage` in un renderer/template dedicato (view Razor o classe di rendering con encoding centralizzato).
- *Dove*: nuovo componente in `Identity.Api/AuthenticationFeature`.
- *Perché*: separare presentazione da sicurezza; ridurre la dimensione del controller.
- *Impatto previsto*: output HTML identico.
- *Rischio dello step*: basso/medio (verificare parità HTML).
- *Test dopo*: test che asseriscono la presenza dei campi/hidden input e dell'encoding.

**Step 4 — Estrarre validazione OAuth e gestione scope.**
- *Cosa*: `IOAuthRequestValidator` con `TryValidateAuthorizationRequest`/scope/redirect/PKCE.
- *Dove*: `Identity.Api/AuthenticationFeature/Services`.
- *Perché*: testabilità unitaria della sicurezza.
- *Impatto/Rischio*: basso.
- *Test dopo*: unit test su scope elevati, redirect non ammessi, PKCE non-S256.

**Step 5 — Estrarre gestione sessione ed emissione token.**
- *Cosa*: `IOAuthSessionManager` (cookie/store) e `IOAuthTokenIssuer` (`CreateScopedTokenResponse`).
- *Dove*: `Identity.Api/AuthenticationFeature/Services`.
- *Perché*: il controller diventa un orchestratore sottile.
- *Impatto/Rischio*: medio (tocca sessione e token); mitigato da test.
- *Test dopo*: integrazione completa authorize→token→me.

# Pattern da applicare

- **Async all the way**.
  - *Problema*: blocco sincrono su async. *Dove*: `Authorize`/`RehydrateLoginResponse`. *Perché adatto*: è la pratica .NET raccomandata su percorsi di richiesta. *Non overengineering*: è la correzione minima e diretta.
- **Extract Service / Separation of Concerns**.
  - *Problema*: SRP violato nel controller. *Dove*: validazione, sessione, token, rendering. *Perché adatto*: isola la sicurezza e la rende testabile. *Non overengineering*: si estraggono solo i 3–4 collaboratori realmente presenti, non un'interfaccia per metodo.
- **View/Template per la pagina di login**.
  - *Problema*: HTML inline nel controller. *Dove*: `RenderLoginPage`. *Perché adatto*: encoding e markup in un unico punto dedicato. *Non overengineering*: usa l'infrastruttura di view già disponibile in ASP.NET.

# Anti-pattern da rimuovere

- **Sync-over-async** (`GetAwaiter().GetResult()` su percorso di richiesta): sostituito con `await`.
- **Fat Controller**: ridotto a orchestratore tramite estrazione dei service.
- **HTML costruito a mano nel controller**: spostato in un renderer/template con encoding centralizzato.
- **Logica di sicurezza annidata in metodi privati non testabili**: estratta in componenti con test unitari.

# Strategia di test

- **Unit test**: `IOAuthRequestValidator` (scope supportati/elevati, redirect HTTPS/localhost, PKCE S256), `IOAuthTokenIssuer` (scope propagati nel token), renderer (encoding degli input).
- **Integration test**: flusso completo Authorization Code + PKCE (authorize → login → code → token → `/api/auth/me`), silent authorize con sessione, logout con revoca, dynamic client registration (abilitata/disabilitata). Estendere `OAuthAndAccessIntegrationTests`.
- **Security test**: PKCE fallita → `invalid_grant`; redirect non ammesso → `invalid_redirect_uri`; scope non consentito → 403 `invalid_scope`; consenso elevato mancante → ripresentazione pagina.
- **Regression test**: parità dei formati di risposta OAuth (`error`/`error_description`, token response).
- **Load/concurrency test (mirato)**: molte richieste concorrenti di silent authorize per verificare l'assenza di starvation dopo lo Step 2.
- **Scenari da coprire *prima* di iniziare**: authorize con/senza sessione, token exchange valido/invalido, logout.

# Rischi del refactor

- **Regressione sul flusso di login/consenso**: è il percorso più sensibile — mitigazione: test di integrazione estesi prima dello Step 2 e parità HTML verificata allo Step 3.
- **Differenze di rendering della pagina**: lo spostamento del markup può alterare campi hidden/encoding — mitigazione: snapshot/asserzioni sui campi del form.
- **Cambio di firma da sincrona ad asincrona**: possibili impatti sul routing/binding — mitigazione: mantenere gli stessi verbi/route e coprire con integrazione.
- **Store in-memory vs PostgreSQL**: comportamenti diversi tra ambienti — mitigazione: test in configurazione simile alla produzione (`PostgreSqlOAuthStores`).

# Strategia di rollback

- Ogni step è un commit isolato e reversibile; nessuna migrazione DB è richiesta dagli step 2–5.
- La correzione async (Step 2) è indipendente e può essere rilasciata da sola; in caso di problemi si fa **revert** del singolo commit senza toccare il resto.
- Deploy progressivo di `Identity.Api` con monitoraggio di error rate, latenza e saturazione del thread pool; rollback all'immagine precedente se peggiora.

# Checklist finale

- [ ] Test di integrazione OAuth estesi e verdi prima delle modifiche.
- [ ] Rimosso `GetAwaiter().GetResult()`; percorso authorize completamente asincrono.
- [ ] Rendering della pagina di login estratto con encoding centralizzato e parità verificata.
- [ ] Validazione OAuth/scope/PKCE estratta in un service con unit test.
- [ ] Gestione sessione ed emissione token estratte; controller ridotto a orchestratore.
- [ ] Formati di risposta OAuth invariati (contract/regression test).
- [ ] Test di sicurezza su PKCE/redirect/scope/consenso elevato verdi.
- [ ] Verifica in staging della stabilità del thread pool sotto carico di silent authorize.
- [ ] Suite `Kin.KinHub.Core.Test` completa verde.
