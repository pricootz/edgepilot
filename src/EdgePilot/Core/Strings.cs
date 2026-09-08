namespace EdgePilot.Core;

internal static class Strings
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<Language, string>> Catalog = new Dictionary<string, IReadOnlyDictionary<Language, string>>
    {
        // Tray menu
        ["tray.settings"] = Map("Impostazioni", "Settings", "Paramètres"),
        ["tray.toggle"] = Map("Mostra / Nascondi pannello", "Show / Hide panel", "Afficher / Masquer le panneau"),
        ["tray.exit"] = Map("Esci", "Exit", "Quitter"),

        // App-level warnings
        ["warning.loadFailed"] = Map(
            "Impossibile leggere le impostazioni salvate. Sono attive quelle predefinite. Premi Applica per salvare nuovamente le preferenze.",
            "Unable to read saved settings. Defaults are active. Press Apply to save your preferences again.",
            "Impossible de lire les paramètres enregistrés. Les valeurs par défaut sont actives. Appuyez sur Appliquer pour enregistrer à nouveau vos préférences."),
        ["warning.autostartCheckFailed"] = Map(
            "Non è stato possibile verificare l'avvio automatico. Controlla le impostazioni.",
            "Could not verify startup-at-login status. Check settings.",
            "Impossible de vérifier le démarrage automatique. Vérifiez les paramètres."),
        ["warning.trayUnavailable"] = Map(
            "Icona nell'area di notifica non disponibile. Puoi riaprire le impostazioni avviando nuovamente EdgePilot.",
            "Tray icon unavailable. You can reopen settings by launching EdgePilot again.",
            "Icône de la zone de notification indisponible. Vous pouvez rouvrir les paramètres en relançant EdgePilot."),

        // CLI (Program.cs)
        ["cli.installedTo"] = Map("EdgePilot installato in {0}", "EdgePilot installed to {0}", "EdgePilot installé dans {0}"),
        ["cli.installFailed"] = Map(
            "Installazione non riuscita. Chiudi EdgePilot e riprova. {0}",
            "Installation failed. Close EdgePilot and try again. {0}",
            "Échec de l'installation. Fermez EdgePilot et réessayez. {0}"),
        ["cli.alreadyRunning"] = Map(
            "EdgePilot è già aperto ma non risponde. Chiudilo e riprova.",
            "EdgePilot is already open but not responding. Close it and try again.",
            "EdgePilot est déjà ouvert mais ne répond pas. Fermez-le et réessayez."),

        // Drive selection
        ["drive.auto"] = Map("Automatico (disco più capiente)", "Automatic (largest disk)", "Automatique (disque le plus grand)"),
        ["drive.unavailableSuffix"] = Map("{0} · non disponibile", "{0} · unavailable", "{0} · indisponible"),

        // Installer / autostart
        ["installer.extractFirst"] = Map(
            "Estrai il pacchetto in una cartella separata prima di installarlo.",
            "Extract the package into a separate folder before installing it.",
            "Extrayez le paquet dans un dossier séparé avant de l'installer."),
        ["installer.startMenuUnavailable"] = Map("Menu Start non disponibile.", "Start menu unavailable.", "Menu Démarrer indisponible."),
        ["desktop.comment"] = Map("Monitoraggio del sistema", "System monitoring", "Surveillance du système"),
        ["autostart.pathUnavailable"] = Map("Percorso dell'app non disponibile.", "App path unavailable.", "Chemin de l'application indisponible."),
        ["autostart.pathTooLong"] = Map(
            "Percorso troppo lungo per l'avvio automatico di Windows.",
            "Path too long for Windows startup registration.",
            "Chemin trop long pour le démarrage automatique de Windows."),
        ["autostart.invalidChars"] = Map(
            "Il percorso dell'app contiene caratteri non supportati.",
            "The app path contains unsupported characters.",
            "Le chemin de l'application contient des caractères non pris en charge."),
        ["autostart.updateFailed"] = Map(
            "Impossibile aggiornare l'avvio automatico.",
            "Unable to update startup-at-login registration.",
            "Impossible de mettre à jour le démarrage automatique."),
        ["appicon.unavailable"] = Map("Icona dell'app non disponibile.", "App icon unavailable.", "Icône de l'application indisponible."),

        // Preferences validation (trace/log only, not shown to the user)
        ["prefs.invalidEdgeMode"] = Map(
            "Bordo o modalità di visualizzazione non supportati.",
            "Unsupported edge or display mode.",
            "Bord ou mode d'affichage non pris en charge."),
        ["prefs.invalidRefresh"] = Map("Intervallo di aggiornamento non supportato.", "Unsupported refresh interval.", "Intervalle de rafraîchissement non pris en charge."),
        ["prefs.invalidSensitivity"] = Map("Sensibilità non supportata.", "Unsupported sensitivity.", "Sensibilité non prise en charge."),
        ["prefs.invalidMetrics"] = Map("Seleziona almeno una metrica valida.", "Select at least one valid metric.", "Sélectionnez au moins une métrique valide."),
        ["prefs.invalidLanguage"] = Map("Lingua non supportata.", "Unsupported language.", "Langue non prise en charge."),
        ["prefs.emptyFile"] = Map("Il file delle impostazioni è vuoto.", "The settings file is empty.", "Le fichier de paramètres est vide."),

        // Uptime unit
        ["time.dayUnit"] = Map("g", "d", "j"),

        // Settings window
        ["settings.title"] = Map("EdgePilot · Impostazioni", "EdgePilot · Settings", "EdgePilot · Paramètres"),
        ["edge.right"] = Map("Destra", "Right", "Droite"),
        ["edge.left"] = Map("Sinistra", "Left", "Gauche"),
        ["edge.top"] = Map("Alto", "Top", "Haut"),
        ["edge.bottom"] = Map("Basso", "Bottom", "Bas"),
        ["mode.hover"] = Map("Al passaggio del mouse", "On hover", "Au survol"),
        ["mode.always"] = Map("Sempre aperto", "Always open", "Toujours ouvert"),
        ["mode.hidden"] = Map("Nascosto", "Hidden", "Masqué"),
        ["refresh.0_5s"] = Map("Ogni 0,5 secondi", "Every 0.5 seconds", "Toutes les 0,5 secondes"),
        ["refresh.1s"] = Map("Ogni secondo", "Every second", "Toutes les secondes"),
        ["refresh.2s"] = Map("Ogni 2 secondi", "Every 2 seconds", "Toutes les 2 secondes"),
        ["refresh.5s"] = Map("Ogni 5 secondi", "Every 5 seconds", "Toutes les 5 secondes"),
        ["sensitivity.precise"] = Map("Precisa", "Precise", "Précise"),
        ["sensitivity.normal"] = Map("Normale", "Normal", "Normale"),
        ["sensitivity.wide"] = Map("Ampia", "Wide", "Large"),
        ["metric.cpu"] = Map("CPU", "CPU", "CPU"),
        ["metric.memory"] = Map("Memoria", "Memory", "Mémoire"),
        ["metric.disk"] = Map("Disco", "Disk", "Disque"),
        ["metric.network"] = Map("Rete", "Network", "Réseau"),
        ["settings.autostart"] = Map("Avvia all'accesso", "Start at login", "Démarrer à la connexion"),
        ["settings.exitButton"] = Map("Esci da EdgePilot", "Exit EdgePilot", "Quitter EdgePilot"),
        ["settings.applyHint"] = Map("Premi Applica per salvare le modifiche.", "Press Apply to save your changes.", "Appuyez sur Appliquer pour enregistrer vos modifications."),
        ["settings.apply"] = Map("Applica", "Apply", "Appliquer"),
        ["settings.selectMetric"] = Map(
            "Seleziona almeno una metrica da mostrare.",
            "Select at least one metric to display.",
            "Sélectionnez au moins une métrique à afficher."),
        ["settings.hiddenWithTray"] = Map(
            "Pannello nascosto. Usa l'icona nell'area di notifica per mostrarlo o riaprire le impostazioni.",
            "Panel hidden. Use the tray icon to show it or reopen settings.",
            "Panneau masqué. Utilisez l'icône de la zone de notification pour l'afficher ou rouvrir les paramètres."),
        ["settings.hiddenNoTray"] = Map(
            "Pannello nascosto. Scegli un'altra modalità per mostrarlo. Chiudendo le impostazioni esci da EdgePilot; al prossimo avvio tornerai qui.",
            "Panel hidden. Choose a different mode to show it. Closing settings exits EdgePilot; you'll return here next time it starts.",
            "Panneau masqué. Choisissez un autre mode pour l'afficher. Fermer les paramètres quitte EdgePilot ; vous reviendrez ici au prochain démarrage."),
        ["settings.saved"] = Map(
            "Impostazioni salvate. Fai clic destro sul pannello per riaprirle.",
            "Settings saved. Right-click the panel to reopen them.",
            "Paramètres enregistrés. Faites un clic droit sur le panneau pour les rouvrir."),
        ["settings.saveFailed"] = Map(
            "Impossibile salvare le impostazioni. Le preferenze attive non sono cambiate. Verifica i permessi di scrittura e lo spazio disponibile, poi riprova.",
            "Unable to save settings. Your active preferences are unchanged. Check write permissions and available disk space, then try again.",
            "Impossible d'enregistrer les paramètres. Vos préférences actives sont inchangées. Vérifiez les autorisations d'écriture et l'espace disponible, puis réessayez."),
        ["settings.customize"] = Map("Personalizza EdgePilot", "Customize EdgePilot", "Personnaliser EdgePilot"),
        ["settings.screenEdge"] = Map("Bordo dello schermo", "Screen edge", "Bord de l'écran"),
        ["settings.displayMode"] = Map("Visualizzazione", "Display mode", "Mode d'affichage"),
        ["settings.visibleMetrics"] = Map("Metriche visibili", "Visible metrics", "Métriques visibles"),
        ["settings.dataRefresh"] = Map("Aggiornamento dei dati", "Data refresh", "Actualisation des données"),
        ["settings.sensitivityLabel"] = Map("Sensibilità di apertura", "Opening sensitivity", "Sensibilité d'ouverture"),
        ["settings.sensitivityHint"] = Map(
            "Ampia: il pannello si apre anche passando vicino alla linguetta. Precisa: occorre avvicinarsi di più al bordo.",
            "Wide: the panel opens even when passing near the tab. Precise: you need to get closer to the edge.",
            "Large : le panneau s'ouvre même en passant près de l'onglet. Précise : il faut s'approcher davantage du bord."),
        ["settings.driveLabel"] = Map("Disco da visualizzare", "Disk to display", "Disque à afficher"),
        ["settings.driveHint"] = Map(
            "Sono elencati i volumi montati e accessibili. Se manca un disco, montalo: l'elenco si aggiorna automaticamente.",
            "Mounted, accessible volumes are listed. If a disk is missing, mount it: the list updates automatically.",
            "Les volumes montés et accessibles sont listés. Si un disque manque, montez-le : la liste se met à jour automatiquement."),
        ["settings.languageLabel"] = Map("Lingua", "Language", "Langue"),
        ["language.auto"] = Map("Automatica (sistema)", "Automatic (system)", "Automatique (système)"),

        // Notch tooltip / rings
        ["ring.disk"] = Map("DISCO", "DISK", "DISQUE"),
        ["ring.network"] = Map("RETE", "NETWORK", "RÉSEAU"),
        ["tooltip.systemMonitoring"] = Map("MONITORAGGIO SISTEMA", "SYSTEM MONITORING", "SURVEILLANCE SYSTÈME"),
        ["tooltip.error"] = Map("ERRORE", "ERROR", "ERREUR"),
        ["tooltip.updateFailed"] = Map("Impossibile aggiornare i dati del sistema.", "Unable to refresh system data.", "Impossible d'actualiser les données système."),
        ["tooltip.retryNext"] = Map("Nuovo tentativo al prossimo aggiornamento.", "Will retry on the next update.", "Nouvelle tentative à la prochaine mise à jour."),
        ["network.yes"] = Map("SÌ", "YES", "OUI"),
        ["network.no"] = Map("NO", "NO", "NON"),
        ["tooltip.processors"] = Map("{0} processori logici", "{0} logical processors", "{0} processeurs logiques"),
        ["tooltip.memory"] = Map("MEMORIA", "MEMORY", "MÉMOIRE"),
        ["bytes.used"] = Map("{0} utilizzati", "{0} used", "{0} utilisés"),
        ["bytes.available"] = Map("{0} disponibili", "{0} available", "{0} disponibles"),
        ["bytes.total"] = Map("{0} totali", "{0} total", "{0} au total"),
        ["tooltip.storage"] = Map("ARCHIVIAZIONE", "STORAGE", "STOCKAGE"),
        ["storage.noDisk"] = Map("Nessun disco disponibile", "No disk available", "Aucun disque disponible"),
        ["storage.unavailable"] = Map("Il disco scelto non è disponibile", "Selected disk unavailable", "Disque sélectionné indisponible"),
        ["storage.chooseInSettings"] = Map("Scegli il disco nelle impostazioni.", "Choose the disk in settings.", "Choisissez le disque dans les paramètres."),
        ["bytes.free"] = Map("{0} liberi", "{0} free", "{0} libres"),
        ["tooltip.network"] = Map("RETE", "NETWORK", "RÉSEAU"),
        ["network.connected"] = Map("CONNESSA", "CONNECTED", "CONNECTÉ"),
        ["network.disconnected"] = Map("DISCONNESSA", "DISCONNECTED", "DÉCONNECTÉ"),
        ["network.noInterface"] = Map("Nessuna interfaccia di rete attiva", "No active network interface", "Aucune interface réseau active"),
        ["network.linkSpeed"] = Map("Velocità collegamento: {0} Mbps", "Link speed: {0} Mbps", "Vitesse de liaison : {0} Mbps"),
        ["network.uptime"] = Map("Tempo di attività: {0}", "Uptime: {0}", "Disponibilité : {0}"),
    };

    private static IReadOnlyDictionary<Language, string> Map(string italian, string english, string french) => new Dictionary<Language, string>
    {
        [Language.Italian] = italian,
        [Language.English] = english,
        [Language.French] = french,
    };
}
