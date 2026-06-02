# SID-in administratiesysteem

ASP.NET Core MVC-project voor het registreren en beheren van interesses van studiekiezers op SID-in beurzen.

## Starten

Voer in de root van dit project uit:

```powershell
docker compose up -d
```

Open daarna `http://localhost:8080`.

## Demo-accounts

| Rol | E-mail | Wachtwoord |
| --- | --- | --- |
| Beursexposant | expo@sidin.local | Expo123! |
| Marketing medewerker | marketing@sidin.local | Marketing123! |
| Marketing teamlead | teamlead@sidin.local | Lead123! |
| Administrator | admin@sidin.local | Admin123! |

## Belangrijke routes

- Mobiele QR-flow: `/Mobile/Register?visitorId=SID-2004&firstName=Mila&lastName=Dumont&birthDate=2007-05-14&school=College%20Ten%20Doorn&studyArea=Humane%20wetenschappen`
- Marketing: `/Dashboard/Interests`
- Aantallen: `/Dashboard/Counts`
- Teamlead studiekiezersbeheer: `/Dashboard/Visitors`
- Admin gebruikersbeheer: `/Admin/Users`
- API meest gekozen opleiding: `/api/stats/most-chosen-program`
- API minst gekozen opleiding: `/api/stats/least-chosen-program`
- API aantal bezoekers per beurs: `/api/stats/fair/by-name/antwerpen/visitor-count`

## Persistentie

De applicatie bewaart alle gegevens in `/app/data/sidin-database.json`. Docker Compose koppelt die map aan een persistent volume `sidin-data`, waardoor registraties bewaard blijven na container-restarts.
