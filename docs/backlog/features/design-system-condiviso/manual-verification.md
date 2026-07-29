# Verifica manuale FEAT-014

Usa questa checklist per confermare i punti del piano che non sono dimostrabili in modo affidabile con i soli test automatici.

## Esito

- Stato complessivo: `Da eseguire`
- Eseguito da: `<nome o team>`
- Data: `<YYYY-MM-DD>`

## Ambiente verificato

- Browser desktop: `<es. Edge / Chrome / Firefox>`
- Browser mobile o emulazione: `<es. Safari iOS / Chrome Android / DevTools>`
- Modalita PWA installata: `si | no`
- Build verificata: `<versione o SHA>`

## Checklist

### Navigazione e routing

- [ ] La floating navigation resta fissata al viewport e non si sposta con lo scroll della pagina.
- [ ] La barra globale resta coerente su Home, KinList, Settings, Version, Release notes, Docs e 404.
- [ ] Le route funzionano con click, URL diretto, refresh e cronologia browser.
- [ ] La barra contestuale compare solo dove registrata dal contratto condiviso e non duplica la barra globale.

### Lingua e tema

- [ ] Italiano predefinito corretto.
- [ ] Inglese funzionante senza chiavi mancanti visibili.
- [ ] Tema `light` corretto.
- [ ] Tema `dark` corretto.
- [ ] Tema `system` corretto senza flash iniziale.
- [ ] `theme-color`, icona e colori PWA coerenti con la palette finale.

### Accessibilita e focus

- [ ] Skip link funzionante.
- [ ] Focus visibile su shell, accordion, bottoni, link, field, dialog e snackbar.
- [ ] `PageScaffold` porta il focus sul titolo pagina al cambio route.
- [ ] Tutorial/dialog/drawer non perdono il focus.
- [ ] Tastiera: frecce della barra flottante funzionanti.
- [ ] Tastiera: tabs e controlli condivisi funzionanti.
- [ ] Le pagine inattive del carosello non sono raggiungibili accidentalmente durante la navigazione.

### Reduced motion e responsive

- [ ] `prefers-reduced-motion` evita animazioni invasive.
- [ ] Viewport stretti corretti.
- [ ] Zoom 200% corretto.
- [ ] Safe area bottom corretta senza sovrapposizione tra contenuto, barra e snackbar.

### Stati applicativi

- [ ] `ProtectedRoute` mostra stato coerente per accesso richiesto / non configurato.
- [ ] `KinListAccessGate` copre loading, offline, sessione scaduta, forbidden, errore, onboarding e ready.
- [ ] Retry e creazione famiglia mantengono focus e significato.
- [ ] Il nome famiglia resta in memoria dopo errore recuperabile.
- [ ] Nessun dato sensibile o `familyId` viene mostrato in modo improprio nella UI.

### Tutorial, snackbar e overlay

- [ ] Tutorial avvio / skip / indietro / avanti / fine corretti.
- [ ] Riavvio tutorial da Settings corretto.
- [ ] Snackbar aggiornamento non copre in modo problematico la barra flottante.
- [ ] Dialog e drawer condivisi non rompono layout o focus.

### PWA e lifecycle

- [ ] App installata correttamente su desktop o mobile.
- [ ] Refresh controllato della versione corretto.
- [ ] Offline: resta disponibile solo la shell pubblica prevista.

## Note

- `<annotazioni, screenshot, problemi residui, follow-up>`
