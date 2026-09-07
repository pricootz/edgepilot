# Integrazione desktop, dischi e pacchetti

## Scelta del disco
Il monitor non deve assumere che il volume più capiente sia quello desiderato. La selezione iniziale resta automatica per compatibilità, ma Impostazioni permette di scegliere un volume tramite nome, percorso e capacità decimale.
La scelta usa il percorso del volume, con confronto senza distinzione tra maiuscole/minuscole su Windows. Non si sceglie automaticamente un sostituto se il disco non è montato.
L’elenco non è più limitato a cinque elementi. Include volumi montati accessibili con capacità maggiore di zero, escludendo RAM e unità ottiche. Non modifica, monta o smonta dischi.

## Tema e icona
Su Linux le impostazioni seguono la variante chiara/scura e i colori esposti dal sistema ad Avalonia. La cornice della finestra resta gestita dal desktop. Non si tratta di controlli GTK/Yaru nativi.
L’icona usa un menu nativo con Impostazioni, Mostra/Nascondi ed Esci.
Linux richiede un desktop con AppIndicator/StatusNotifier. Anche se l’icona non è visibile, riavviare l’eseguibile riapre le impostazioni tramite la singola istanza.
Mostra/Nascondi dal menu è temporaneo; Applica salva la modalità per i prossimi avvii.

## Avvio all’accesso
Disattivato per default. Windows registra soltanto la voce EdgePilot nella chiave Run dell’utente corrente. Linux usa ~/.config/autostart/io.github.pricootz.EdgePilot.desktop (rispetta XDG_CONFIG_HOME).
Nessuna elevazione, servizio o modifica per altri utenti. Se il salvataggio delle preferenze fallisce, la registrazione precedente viene ripristinata.
Installare in una cartella stabile prima di attivarlo. L’installazione aggiorna il comando di un avvio automatico già attivo, senza attivarlo se era disattivato.

## Distribuzione
La CI produce un archivio Windows ZIP e un archivio Linux tar.gz x64, più checksum SHA-256. Sono pacchetti autonomi con runtime .NET incluso, non installer di sistema né pacchetti firmati.
Il comando --install copia l’app nel profilo e crea il collegamento Start/Applicazioni. Non richiede privilegi amministrativi.
L’installer richiede di chiudere le versioni precedenti prima dell’aggiornamento. Per rimuovere l’app: disattivare l’avvio automatico, uscire, rimuovere cartella e collegamento.

## Verifiche
GitHub Actions esegue build e suite UX su Windows/Ubuntu; decodifica l’icona, verifica disco selezionato/parity, volume assente, selettore aggiornabile, persistenza e rollback dell’avvio.
I pacchetti vengono avviati con una finestra reale (Xvfb su Linux) e chiusi automaticamente; viene inoltre eseguita l’installazione per utente sui runner.
Resta da verificare nel desktop reale dell’utente la resa di tema, menu tray e l’avvio dopo logout/login. Le prove CI non simulano tutti i compositor né un accesso utente completo.
