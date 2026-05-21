---
id: FEAT-005
type: feature
status: planned
priority: high
created_at: 2026-05-21
related:
  - FEAT-003
  - TASK-001
  - TASK-002
  - TASK-003
  - TASK-004
  - CR-001
  - RFC-001
---

# FEAT-005 - KinAi tool lettura ricette e deep link Core React

## Descrizione

Estendere KinAi con nuovi tool read-only orientati a KinRecipe:

1. visualizzazione degli elementi di una lista della spesa;
2. elenco dei libri di ricette;
3. elenco delle ricette di un libro;
4. ingredienti mancanti per una ricetta specifica;
5. link diretto alla pagina dettaglio ricetta sulla Static Web App di `Kin.KinHub.Core.React`.

La richiesta e' un follow-up di `FEAT-003`: il flusso di approvazione ed esecuzione tool esiste gia', ma oggi copre solo una parte minima del dominio shopping list.

## Stato corrente

| Area | Evidenza |
|---|---|
| Catalogo tool OpenAI | `OpenAiChatService` dichiara `list_recipe_books`, ma `KinHubChatToolExecutor` non lo esegue ancora |
| Tool disponibili | L'executor supporta solo `list_shopping_lists`, `create_shopping_list`, `add_shopping_list_item` |
| Dati ricette/liste | Backend e Core React hanno gia' servizi e route per recipe books, recipes, shopping list items e missing ingredients |
| Deep link cross-app | KinAi non ha una base URL configurata per la SWA di Core React |
| Rendering messaggi chat | `ConversationDetailPage.tsx` stampa testo semplice, quindi eventuali URL non diventano azioni cliccabili |

## Decisioni architetturali

| Decisione | Scelta |
|---|---|
| Punto di integrazione | Estendere il catalogo tool in `OpenAiChatService` e l'esecuzione in `KinHubChatToolExecutor` |
| Accesso ai dati | Riutilizzare i servizi business esistenti (`IRecipeBookService`, `IRecipeService`, `IShoppingListItemService`, `IRecipeMissingIngredientsService`) |
| Scope tool | Tool solo di lettura; nessuna mutazione aggiuntiva in questa iterazione |
| Formato risposta | Risposte assistant concise con liste leggibili e, per le ricette, URL completo verso Core React |
| Deep link ricetta | Costruire il link dal route template esistente `/recipe-books/{bookId}/recipes/{recipeId}` e dalla variabile ambiente `VITE_CORE_URL` lato KinAi |
| Rendering UI | Rendere cliccabili gli URL nei messaggi assistant senza introdurre un secondo canale di payload custom |

## Dipendenze

- `IRecipeBookService`
- `IRecipeService`
- `IShoppingListItemService`
- `IRecipeMissingIngredientsService`
- `OpenAiChatService` per schema e descrizioni dei tool
- `ConversationDetailPage.tsx` per rendering link
- pipeline deploy KinAi per variabile ambiente della SWA Core React

## Rischi

- base URL della SWA Core React non configurata o incoerente tra ambienti;
- output troppo verbosi se una lista o un ricettario contiene molti elementi;
- esposizione di link non cliccabili se il rendering UI non viene aggiornato insieme al backend;
- mismatch tra tool dichiarati a OpenAI e tool realmente supportati dall'executor.

## Acceptance Criteria

- [ ] KinAi puo' mostrare gli elementi di una shopping list esistente
- [ ] KinAi puo' elencare i recipe book della famiglia
- [ ] KinAi puo' elencare le ricette contenute in un recipe book
- [ ] KinAi puo' mostrare gli ingredienti mancanti per una ricetta specifica
- [ ] Quando una risposta cita una ricetta salvata, il messaggio include il link alla pagina dettaglio su `Kin.KinHub.Core.React`
- [ ] Il link ricetta e' cliccabile nella UI KinAi
- [ ] I nuovi tool rispettano autorizzazione e boundary della famiglia corrente

## Fasi implementative

1. Estendere il catalogo tool AI e il contratto di esecuzione per le nuove query recipe/shopping list.
2. Implementare nel backend i tool read-only riusando i servizi applicativi esistenti.
3. Costruire i deep link ricetta verso Core React e includerli nelle risposte assistant.
4. Aggiornare la UI KinAi per rendere cliccabili i link e configurare il deploy con la base URL corretta.

## Moduli / file impattati

- `src/Infrastructures/Kin.KinHub.Core.OpenAi/ChatFeature/Services/OpenAiChatService.cs`
- `src/Businesses/Kin.KinHub.Core.Business/ChatFeature/Services/KinHubChatToolExecutor.cs`
- `src/Businesses/Kin.KinHub.Core.Business/ChatFeature/Models/ChatToolExecutionResult.cs`
- `src/Businesses/Kin.KinHub.Core.Business/ServiceCollectionExtensions.cs`
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Interfaces/IRecipeBookService.cs`
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Interfaces/IRecipeService.cs`
- `src/Presentations/Kin.KinHub.KinAi.React/src/features/chat/pages/ConversationDetailPage.tsx`
- `src/Presentations/Kin.KinHub.KinAi.React/src/types/index.ts`
- `.github/workflows/deploy-kinai-frontend.yml`

