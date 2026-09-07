# Impostazioni di EdgePilot

Apri le impostazioni con clic destro sul pannello oppure avvia con --settings.

- Bordo: destra, sinistra, alto, basso.
- Visualizzazione: al passaggio del mouse, sempre aperto, nascosto.
- Metriche: CPU, memoria, disco, rete. È richiesta almeno una selezione. Il pannello si adatta al numero di metriche; i dati continuano a essere raccolti, così riattivarle non perde informazioni.
- Aggiornamento: ogni 0,5, 1, 2 o 5 secondi. La nuova frequenza viene usata dalla prossima attesa del servizio; un’attesa già in corso termina con l’intervallo precedente.
- Sensibilità: precisa, normale, ampia. Cambia la zona sensibile al mouse, non la forma della linguetta. Normale mantiene il comportamento precedente.

Premi Applica per salvare e applicare. Le impostazioni preesistenti restano compatibili: tutti i dati visibili, aggiornamento ogni secondo e sensibilità normale.

Il salvataggio è atomico. Valori non validi o nessuna metrica selezionata non sostituiscono le preferenze valide.

Verifiche automatiche su GitHub Actions: build e controlli UX su Windows e Ubuntu, combinazioni delle metriche sui quattro bordi, zone del mouse, salvataggio e compatibilità dei file precedenti. Verificare sul desktop la sensibilità preferita.

L’icona nell’area di notifica e l’avvio automatico restano il passo successivo.
