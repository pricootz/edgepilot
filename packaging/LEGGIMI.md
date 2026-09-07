# EdgePilot per Windows e Ubuntu (x64)

Il pacchetto include .NET: non servono SDK, VS Code o un terminale sempre aperto.
Estrai tutti i file insieme, non soltanto l’eseguibile. Chiudi la versione precedente prima di aggiornare.

## Windows
Avvia EdgePilot.exe. Per installarlo nel tuo profilo e aggiungerlo al menu Start, fai clic destro su Installa.ps1 e scegli Esegui con PowerShell. Non sono necessari privilegi amministrativi.
Se preferisci il terminale, dalla cartella estratta esegui:
    .\EdgePilot.exe --install
L’installazione si trova in %LOCALAPPDATA%\Programs\EdgePilot. Apri EdgePilot dal menu Start.

## Ubuntu
Serve una sessione desktop grafica. Dalla cartella estratta:
    ./EdgePilot
Per installarlo nel tuo profilo e aggiungerlo al menu Applicazioni:
    bash installa.sh
La cartella installata è ~/.local/share/edgepilot. Le dipendenze grafiche del desktop (X11/XWayland, fontconfig e librerie di sistema) restano necessarie.
L’icona di notifica richiede il supporto AppIndicator/StatusNotifier del desktop. Su GNOME può servire l’estensione AppIndicator.

## Uso
Clic destro sul pannello o menu dell’icona → Impostazioni.
Un secondo avvio apre le impostazioni dell’istanza esistente.
Il menu dell’icona permette anche Mostra/Nascondi ed Esci.
Avvia all’accesso è disattivato per impostazione predefinita. Attivalo dopo aver installato l’app in una posizione stabile.
Se l’icona non è disponibile, le impostazioni mantengono il percorso di recupero della modalità nascosta. Puoi sempre rilanciare l’app per riaprirle.

## Disco
In Impostazioni → Disco da visualizzare scegli il volume tramite nome, percorso e capacità.
La selezione è salvata per percorso (lettera unità su Windows, punto di montaggio su Ubuntu).
Se il volume manca non viene sostituito con un altro: compare non disponibile.
Sono elencati i volumi montati, accessibili e con capacità disponibile, anche oltre i primi cinque.
La capacità nel selettore usa TB/GB decimali, come le etichette commerciali.

## Tema Ubuntu
Le impostazioni seguono il tema chiaro/scuro del desktop e i colori esposti ad Avalonia, con decorazioni di finestra del sistema.
Il pannello notch mantiene il proprio aspetto. I controlli delle impostazioni sono Avalonia, non GTK/Yaru nativi.

## Rimozione
Disattiva Avvia all’accesso, premi Applica ed esci. Puoi poi rimuovere la cartella installata e il collegamento dal menu.
Le preferenze restano nella cartella dati dell’utente, in EdgePilot/settings.json.
