# Authentification et adhésion

## Écrans d’entrée

1. **Créer une entreprise** : création du compte Directeur, collecte des informations requises, création de l’identifiant d’entreprise et initialisation de la base centrale.
2. **Rejoindre une entreprise** : création d’un compte utilisateur, saisie de l’identifiant d’entreprise et soumission d’une demande d’adhésion.
3. **Se connecter** : accès d’un utilisateur déjà autorisé.

## Création d’entreprise

Le créateur devient Directeur et propriétaire administratif de l’entreprise. La base centrale, les paramètres initiaux, les rôles et les permissions par défaut sont initialisés côté serveur.

## Demande d’adhésion

La demande est transmise au serveur de l’entreprise. Le Directeur l’accepte ou la refuse, puis attribue un rôle, des permissions et une licence. Si le serveur n’est pas joignable, la demande ne peut pas être traitée à distance.

## Multi-appareils

Un même compte peut être utilisé sur plusieurs appareils dans les limites définies par l’entreprise. À la première connexion, l’appareil est authentifié, vérifié et enregistré. Les connexions suivantes contrôlent à nouveau la validité du compte, de la session, de la licence et de l’appareil.

## Compte Directeur

Le Directeur utilise le même mécanisme d’accès que les autres utilisateurs depuis ses appareils secondaires. Son téléphone ou son second ordinateur ne contourne pas l’indisponibilité du serveur central.

## Rotation et détection de réutilisation

Les refresh tokens sont opaques et stockés sous forme de hash SHA-256. Chaque utilisation valide effectue une rotation : l'ancien enregistrement est révoqué et lié au hash du nouveau token. Si un ancien token déjà remplacé est présenté de nouveau, le serveur considère qu'il peut avoir été compromis, révoque les sessions de cet utilisateur sur l'appareil concerné et écrit un événement d'audit `security.refresh-token-reuse`. Les jetons d'accès associés à ces sessions deviennent alors inutilisables à la requête suivante grâce au contrôle du `session_id`.

Une répétition légitime d'une ancienne requête de refresh après une rotation peut donc obliger l'utilisateur à se reconnecter sur cet appareil. Le client doit traiter un refus de refresh comme une demande de nouvelle authentification, sans réessayer en boucle le même ancien token.
