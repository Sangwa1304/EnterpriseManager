# Licences, rôles et permissions

## Gouvernance

Le Directeur est le seul acteur autorisé à :
- accepter ou refuser les demandes d’adhésion ;
- attribuer, renouveler, suspendre ou révoquer des licences ;
- créer et modifier les rôles ;
- attribuer les permissions ;
- gérer les appareils enregistrés ;
- consulter les journaux d’activité administratifs.

## Modèle conceptuel

Entités initialement envisagées :
- Companies
- Users
- CompanyMemberships
- Devices
- UserSessions
- Licenses
- Roles
- Permissions

Les relations et contraintes exactes seront définies dans le modèle de domaine et le schéma de données.

## Séparation

Chaque requête métier doit être rattachée à une entreprise et à une appartenance autorisée. L’identifiant d’entreprise fourni par le client ne constitue jamais à lui seul une preuve d’accès.
