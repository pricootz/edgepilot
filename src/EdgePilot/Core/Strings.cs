namespace EdgePilot.Core;

internal static class Strings
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<Language, string>> Catalog =
        new Dictionary<string, IReadOnlyDictionary<Language, string>>
        {
            // Tray menu
            ["tray.settings"] = Map("Impostazioni", "Settings", "Paramètres"),
            ["tray.toggle"] = Map("Mostra / Nascondi pannello", "Show / Hide panel", "Afficher / Masquer le panneau"),
            ["tray.exit"] = Map("Esci", "Exit", "Quitter"),

            // App-level warnings
            ["warning.loadFailed"] = Map(
                "Impossibile leggere le impostazioni salvate. Sono attive quelle predefinite. Salva nuovamente le preferenze per ripristinare il file.",
                "Unable to read saved settings. Defaults are active. Save your preferences again to restore the file.",
                "Impossible de lire les paramètres enregistrés. Les valeurs par défaut sont actives. Enregistrez à nouveau vos préférences pour restaurer le fichier."),
            ["warning.autostartCheckFailed"] = Map(
                "Non è stato possibile verificare l'avvio automatico. Controlla le impostazioni.",
                "Could not verify startup-at-login status. Check settings.",
                "Impossible de vérifier le démarrage automatique. Vérifiez les paramètres."),
            ["warning.trayUnavailable"] = Map(
                "Icona nell'area di notifica non disponibile. Puoi riaprire le impostazioni avviando nuovamente EdgePilot.",
                "Tray icon unavailable. You can reopen settings by launching EdgePilot again.",
                "Icône de la zone de notification indisponible. Vous pouvez rouvrir les paramètres en relançant EdgePilot."),

            // CLI
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
            ["desktop.comment"] = Map(
                "Superficie desktop edge-native",
                "Edge-native desktop surface",
                "Surface de bureau native au bord de l'écran"),
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

            // Preferences validation
            ["prefs.invalidEdgeMode"] = Map(
                "Bordo o modalità di visualizzazione non supportati.",
                "Unsupported edge or display mode.",
                "Bord ou mode d'affichage non pris en charge."),
            ["prefs.invalidRefresh"] = Map("Intervallo di aggiornamento non supportato.", "Unsupported refresh interval.", "Intervalle de rafraîchissement non pris en charge."),
            ["prefs.invalidSensitivity"] = Map("Sensibilità non supportata.", "Unsupported sensitivity.", "Sensibilité non prise en charge."),
            ["prefs.invalidMetrics"] = Map("Seleziona almeno una metrica valida.", "Select at least one valid metric.", "Sélectionnez au moins une métrique valide."),
            ["prefs.invalidLanguage"] = Map("Lingua non supportata.", "Unsupported language.", "Langue non prise en charge."),
            ["prefs.emptyFile"] = Map("Il file delle impostazioni è vuoto.", "The settings file is empty.", "Le fichier de paramètres est vide."),

            // Time
            ["time.dayUnit"] = Map("g", "d", "j"),

            // Common settings values
            ["settings.title"] = Map("EdgePilot · Impostazioni", "EdgePilot · Settings", "EdgePilot · Paramètres"),
            ["settings.headerSubtitle"] = Map("Impostazioni", "Settings", "Paramètres"),
            ["settings.sidebarHeading"] = Map("IMPOSTAZIONI", "SETTINGS", "PARAMÈTRES"),
            ["edge.right"] = Map("Destra", "Right", "Droite"),
            ["edge.left"] = Map("Sinistra", "Left", "Gauche"),
            ["edge.top"] = Map("Alto", "Top", "Haut"),
            ["edge.bottom"] = Map("Basso", "Bottom", "Bas"),
            ["mode.hover"] = Map("Al passaggio", "On hover", "Au survol"),
            ["mode.always"] = Map("Sempre visibile", "Always visible", "Toujours visible"),
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
            ["language.auto"] = Map("Automatica (sistema)", "Automatic (system)", "Automatique (système)"),

            // Navigation
            ["nav.edge"] = Map("Bordo", "Edge", "Bord"),
            ["nav.monitor"] = Map("Monitor", "Monitor", "Moniteur"),
            ["nav.behavior"] = Map("Comportamento", "Behavior", "Comportement"),
            ["nav.startup"] = Map("Avvio", "Startup", "Démarrage"),
            ["nav.about"] = Map("Informazioni", "About", "À propos"),

            // Footer / save state
            ["settings.status.saved"] = Map("Tutto salvato.", "Everything is saved.", "Tout est enregistré."),
            ["settings.status.unsaved"] = Map("Modifiche non salvate.", "Unsaved changes.", "Modifications non enregistrées."),
            ["settings.status.reset"] = Map("Modifiche annullate.", "Changes reverted.", "Modifications annulées."),
            ["settings.saveChanges"] = Map("Salva modifiche", "Save changes", "Enregistrer"),
            ["settings.resetChanges"] = Map("Annulla", "Reset", "Annuler"),
            ["settings.selectMetric"] = Map(
                "Seleziona almeno una metrica da mostrare.",
                "Select at least one metric to display.",
                "Sélectionnez au moins une métrique à afficher."),
            ["settings.hiddenWithTray"] = Map(
                "Pannello nascosto. Puoi riaprirlo dall'area di notifica.",
                "Panel hidden. You can reopen it from the tray icon.",
                "Panneau masqué. Vous pouvez le rouvrir depuis l'icône de la zone de notification."),
            ["settings.hiddenNoTray"] = Map(
                "Pannello nascosto. Chiudendo le impostazioni esci da EdgePilot; al prossimo avvio tornerai qui.",
                "Panel hidden. Closing settings exits EdgePilot; you will return here on the next launch.",
                "Panneau masqué. Fermer les paramètres quitte EdgePilot ; vous reviendrez ici au prochain lancement."),
            ["settings.saved"] = Map("Impostazioni salvate.", "Settings saved.", "Paramètres enregistrés."),
            ["settings.saveFailed"] = Map(
                "Impossibile salvare le impostazioni. Verifica permessi e spazio disponibile, poi riprova.",
                "Unable to save settings. Check permissions and available disk space, then try again.",
                "Impossible d'enregistrer les paramètres. Vérifiez les autorisations et l'espace disponible, puis réessayez."),
            ["settings.browserFailed"] = Map(
                "Impossibile aprire il browser. Visita github.com/pricootz/edgepilot.",
                "Unable to open the browser. Visit github.com/pricootz/edgepilot.",
                "Impossible d'ouvrir le navigateur. Visitez github.com/pricootz/edgepilot."),

            // Edge page
            ["edge.pageTitle"] = Map("Bordo e presenza", "Edge & presence", "Bord et présence"),
            ["edge.pageSubtitle"] = Map(
                "Definisci dove vive EdgePilot e quanto deve essere presente sul desktop.",
                "Choose where EdgePilot lives and how present it should be on your desktop.",
                "Définissez où vit EdgePilot et à quel point il doit être présent sur votre bureau."),
            ["edge.positionTitle"] = Map("Posizione sullo schermo", "Screen position", "Position à l'écran"),
            ["edge.positionDescription"] = Map(
                "La preview cambia insieme alla scelta e mostra dove comparirà la linguetta.",
                "The preview follows your selection and shows where the tab will appear.",
                "L'aperçu suit votre choix et montre où l'onglet apparaîtra."),
            ["edge.positionLabel"] = Map("Posizione", "Position", "Position"),
            ["edge.positionHint"] = Map("Scegli il bordo da cui EdgePilot deve emergere.", "Choose the edge EdgePilot should emerge from.", "Choisissez le bord depuis lequel EdgePilot doit apparaître."),
            ["edge.behaviorTitle"] = Map("Comportamento del pannello", "Panel behavior", "Comportement du panneau"),
            ["edge.behaviorDescription"] = Map(
                "Al passaggio mantiene EdgePilot discreto. Sempre visibile lo lascia aperto. Nascosto lo rimuove dal bordo finché non lo riapri dalla tray o dalle impostazioni.",
                "On hover keeps EdgePilot discreet. Always visible keeps it open. Hidden removes it from the edge until you reopen it from the tray or settings.",
                "Au survol garde EdgePilot discret. Toujours visible le maintient ouvert. Masqué le retire du bord jusqu'à sa réouverture depuis la zone de notification ou les paramètres."),

            // Monitor page
            ["monitor.pageTitle"] = Map("Monitor di sistema", "System monitor", "Moniteur système"),
            ["monitor.pageSubtitle"] = Map(
                "Decidi quali dati del modulo System devono comparire nella notch.",
                "Choose which System module data should appear in the notch.",
                "Choisissez les données du module Système à afficher dans l'encoche."),
            ["monitor.metricsTitle"] = Map("Metriche visibili", "Visible metrics", "Métriques visibles"),
            ["monitor.metricsDescription"] = Map(
                "Fai clic sull'intera scheda per attivare o disattivare una metrica. La notch si ridimensiona automaticamente.",
                "Click the whole card to enable or disable a metric. The notch resizes automatically.",
                "Cliquez sur toute la carte pour activer ou désactiver une métrique. L'encoche se redimensionne automatiquement."),
            ["metric.cpu.description"] = Map("Carico del processore in tempo reale", "Real-time processor load", "Charge du processeur en temps réel"),
            ["metric.memory.description"] = Map("Memoria RAM attualmente utilizzata", "Current RAM usage", "Mémoire RAM actuellement utilisée"),
            ["metric.disk.description"] = Map("Spazio occupato sul volume scelto", "Used space on the selected volume", "Espace utilisé sur le volume sélectionné"),
            ["metric.network.description"] = Map("Traffico di download e upload", "Download and upload traffic", "Trafic de téléchargement et d'envoi"),
            ["monitor.diskTitle"] = Map("Volume per la metrica Disco", "Volume for the Disk metric", "Volume pour la métrique Disque"),
            ["monitor.diskDescription"] = Map(
                "Questa scelta compare solo quando la metrica Disco è attiva. EdgePilot conserva comunque la selezione se la disattivi temporaneamente.",
                "This option appears only while the Disk metric is enabled. EdgePilot still keeps your selection if you temporarily disable it.",
                "Cette option apparaît uniquement lorsque la métrique Disque est active. EdgePilot conserve votre sélection si vous la désactivez temporairement."),

            // Behavior page
            ["behavior.pageTitle"] = Map("Comportamento", "Behavior", "Comportement"),
            ["behavior.pageSubtitle"] = Map(
                "Regola quanto spesso EdgePilot aggiorna i dati, quanto facilmente reagisce al bordo e la lingua dell'interfaccia.",
                "Control how often EdgePilot refreshes data, how easily it reacts to the edge, and the interface language.",
                "Réglez la fréquence d'actualisation, la sensibilité au bord et la langue de l'interface."),
            ["behavior.refreshTitle"] = Map("Frequenza di aggiornamento", "Refresh frequency", "Fréquence d'actualisation"),
            ["behavior.refreshDescription"] = Map(
                "Un intervallo breve rende i valori più reattivi. Un intervallo più lungo riduce leggermente il lavoro in background.",
                "A shorter interval makes values more responsive. A longer interval slightly reduces background work.",
                "Un intervalle court rend les valeurs plus réactives. Un intervalle plus long réduit légèrement le travail en arrière-plan."),
            ["behavior.refreshLabel"] = Map("Aggiorna i dati", "Refresh data", "Actualiser les données"),
            ["behavior.sensitivityTitle"] = Map("Sensibilità al bordo", "Edge sensitivity", "Sensibilité au bord"),
            ["behavior.sensitivityDescription"] = Map(
                "Precisa richiede di arrivare vicino alla linguetta. Ampia crea una zona di aggancio più generosa. Normale è il compromesso consigliato.",
                "Precise requires getting close to the tab. Wide creates a larger activation zone. Normal is the recommended balance.",
                "Précise exige de s'approcher de l'onglet. Large crée une zone d'activation plus généreuse. Normale est le compromis recommandé."),
            ["behavior.languageTitle"] = Map("Lingua dell'interfaccia", "Interface language", "Langue de l'interface"),
            ["behavior.languageDescription"] = Map(
                "Automatica segue la lingua del sistema quando disponibile. Puoi forzare Italiano, English o Français.",
                "Automatic follows the system language when available. You can force Italiano, English, or Français.",
                "Automatique suit la langue du système lorsqu'elle est disponible. Vous pouvez forcer Italiano, English ou Français."),

            // Startup page
            ["startup.pageTitle"] = Map("Avvio e sessione", "Startup & session", "Démarrage et session"),
            ["startup.pageSubtitle"] = Map(
                "Scegli come EdgePilot entra nella sessione e quando deve terminare completamente.",
                "Choose how EdgePilot enters your session and when it should shut down completely.",
                "Choisissez comment EdgePilot démarre dans votre session et quand il doit se fermer complètement."),
            ["startup.autostartTitle"] = Map("Avvio automatico", "Start at login", "Démarrage automatique"),
            ["startup.autostartDescription"] = Map(
                "Abilita l'avvio per l'utente corrente. EdgePilot resterà disponibile senza doverlo lanciare manualmente a ogni accesso.",
                "Enable startup for the current user. EdgePilot will remain available without being launched manually after every sign-in.",
                "Activez le démarrage pour l'utilisateur actuel. EdgePilot restera disponible sans lancement manuel à chaque connexion."),
            ["startup.autostartLabel"] = Map(
                "Avvia EdgePilot quando accedo al sistema",
                "Start EdgePilot when I sign in",
                "Démarrer EdgePilot à ma connexion"),
            ["startup.sessionTitle"] = Map("Sessione corrente", "Current session", "Session actuelle"),
            ["startup.sessionDescription"] = Map(
                "Chiude completamente EdgePilot, inclusi pannello e processo in background.",
                "Completely closes EdgePilot, including the panel and background process.",
                "Ferme complètement EdgePilot, y compris le panneau et le processus en arrière-plan."),
            ["startup.exitButton"] = Map("Esci da EdgePilot", "Exit EdgePilot", "Quitter EdgePilot"),

            // About page
            ["about.pageTitle"] = Map("Informazioni", "About", "À propos"),
            ["about.pageSubtitle"] = Map(
                "Il progetto, chi lo sviluppa e i principi su cui viene costruito.",
                "The project, who builds it, and the principles behind it.",
                "Le projet, son auteur et les principes sur lesquels il est construit."),
            ["about.productDescription"] = Map(
                "Una superficie desktop edge-native per Windows e Linux, progettata per mostrare informazioni e azioni solo quando servono.",
                "An edge-native desktop surface for Windows and Linux, designed to surface information and actions only when they matter.",
                "Une surface de bureau native au bord de l'écran pour Windows et Linux, conçue pour afficher informations et actions uniquement lorsqu'elles comptent."),
            ["about.authorTitle"] = Map("Autore", "Author", "Auteur"),
            ["about.authorLine"] = Map("Creato e mantenuto da @pricootz", "Created and maintained by @pricootz", "Créé et maintenu par @pricootz"),
            ["about.authorDescription"] = Map(
                "EdgePilot è un progetto indipendente open source. Idee, sviluppo, direzione del prodotto e manutenzione principale fanno capo a @pricootz, con contributi della community tramite GitHub.",
                "EdgePilot is an independent open-source project. Product direction, development, and primary maintenance are led by @pricootz, with community contributions through GitHub.",
                "EdgePilot est un projet open source indépendant. La direction du produit, le développement et la maintenance principale sont assurés par @pricootz, avec des contributions de la communauté via GitHub."),
            ["about.repoButton"] = Map("Repository GitHub", "GitHub repository", "Dépôt GitHub"),
            ["about.profileButton"] = Map("Profilo @pricootz", "@pricootz profile", "Profil @pricootz"),
            ["about.philosophyTitle"] = Map("Filosofia del progetto", "Project philosophy", "Philosophie du projet"),
            ["about.localTitle"] = Map("Locale per impostazione predefinita", "Local by default", "Local par défaut"),
            ["about.localDescription"] = Map(
                "Nessun account, nessun backend cloud obbligatorio e nessun uploader di telemetria.",
                "No account, no required cloud backend, and no telemetry uploader.",
                "Aucun compte, aucun backend cloud obligatoire et aucun envoi de télémétrie."),
            ["about.edgeNativeTitle"] = Map("Edge-native", "Edge-native", "Edge-native"),
            ["about.edgeNativeDescription"] = Map(
                "EdgePilot vive sul bordo dello schermo e punta a ridurre finestre, dashboard e notifiche tradizionali.",
                "EdgePilot lives on the screen edge and aims to reduce traditional windows, dashboards, and notifications.",
                "EdgePilot vit au bord de l'écran et vise à réduire les fenêtres, tableaux de bord et notifications traditionnels."),
            ["about.evolvingTitle"] = Map("In evoluzione", "Evolving", "En évolution"),
            ["about.evolvingDescription"] = Map(
                "La preview attuale parte dal monitor di sistema; i prossimi moduli allargheranno il concetto senza trasformarlo in un pannello generico.",
                "The current preview starts with system monitoring; upcoming modules will expand the idea without turning it into a generic dashboard.",
                "La version actuelle commence par le monitoring système ; les prochains modules élargiront le concept sans en faire un tableau de bord générique."),
            ["about.versionTitle"] = Map("Versione", "Version", "Version"),
            ["about.versionDescription"] = Map(
                "Preview in sviluppo attivo. Le API, il layout e alcune funzioni possono ancora cambiare prima della prima release stabile.",
                "Preview under active development. APIs, layout, and some features may still change before the first stable release.",
                "Preview en développement actif. Les API, la mise en page et certaines fonctions peuvent encore changer avant la première version stable."),

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

    private static IReadOnlyDictionary<Language, string> Map(string italian, string english, string french) =>
        new Dictionary<Language, string>
        {
            [Language.Italian] = italian,
            [Language.English] = english,
            [Language.French] = french,
        };
}
