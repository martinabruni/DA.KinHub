# Kin.KinHub

Progetto .NET 9, Clean Architecture: layer Domain / Business / Infrastructure / Presentation / Shared.Kernel.

## Architecture Decision Record

### ADR-001 — Facade service + `Result<T>`, senza eccezioni di dominio propagate ai controller

**Contesto.** I service applicativi devono comunicare esiti di fallimento (not found, conflict, validation, forbidden, unauthenticated) senza affidarsi al flusso di controllo tramite eccezioni fino al layer di presentazione.

**Decisione.**
- I service espongono una facade sottile che ritorna `Result<T>` (`Kin.KinHub.Shared.Kernel.Common.Result<T>`). La facade delega ai CQRS handler, ciascuno dei quali ritorna anch'esso `Result<T>`.
- Le eccezioni di dominio (`Kin.KinHub.Shared.Kernel.Exceptions.EntityNotFoundException`, `DuplicateEntityException`, `DomainValidationException`, e più in generale `SharedDomainException`) vengono catturate **dentro** l'handler/service e mappate alla factory `Result` appropriata (`NotFound`, `Conflict`, `ValidationError`, ecc.). Non si propagano al controller.
- I controller ricevono sempre un `Result<T>` e lo traducono in HTTP tramite `SharedHttpResultMapper`.

**Mappatura HTTP (invariante).** `ResultStatus.Unauthorized` è polisemico e viene disambiguato dal mapper:
- Core / KinList / KinRecipe → HTTP **403 Forbidden** (`unauthorizedIsForbidden: true`). Le factory `Result.Forbidden` esprimono questo caso.
- Identity → HTTP **401 Unauthorized** (`unauthorizedIsForbidden: false`). La factory `Result.Unauthenticated` esprime questo caso.

Le tre factory (`Unauthorized`, `Forbidden`, `Unauthenticated`) condividono lo stesso `ResultStatus.Unauthorized`; è il mapper del contesto a decidere il codice HTTP. Non modificare questa mappatura senza aggiornare i test dei contratti HTTP.

**Dove è applicato.** Riferimento canonico: il KinList context (`KinListService`). Lo stesso pattern è applicato in Core.Business (`KinHubFamilyService`, `KinHubServiceService` e i relativi handler) e in Identity.Business (`LoginUserHandler`, `RegisterUserHandler`, `DeleteUserHandler`, `RefreshTokenHandler`, `UpdateUserEmailHandler`, `UpdateUserPasswordHandler`, `GetCurrentUserHandler`, `LogoutUserHandler`).

**Conseguenze.** I tipi condivisi (`Result<T>`, `ResultStatus`, le eccezioni di dominio) vivono solo in `Shared.Kernel`; non esistono copie locali per contesto.
