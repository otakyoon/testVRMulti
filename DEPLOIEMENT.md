# Déploiement et lancement d'une session

Tableau blanc collaboratif VR — Unity 6000.6.0f1, Mirror 96.11.3, réseau local uniquement.

> **État au 19/09/2026.** La scène est montée et le projet compile, mais
> l'application n'a encore **jamais tourné avec deux casques**. Le tracé sur le
> tableau n'a pas été validé sur matériel réel. Les étapes marquées *(à
> valider)* sont celles qui n'ont pas encore été confrontées au terrain :
> attendez-vous à devoir ajuster. Le reste est vérifié.

---

## 1. Ce qu'il faut avant de commencer

**Matériel**

- Deux casques autonomes compatibles OpenXR (Meta Quest 2 / 3 / Pro).
- Un PC Windows avec Unity 6000.6.0f1 pour produire le build.
- Un point d'accès Wi-Fi que vous contrôlez. **Pas le Wi-Fi de
  l'établissement** — voir la section 6.

**Logiciels sur le PC**

- Unity 6000.6.0f1 avec le module *Android Build Support*, y compris *OpenJDK*
  et *Android SDK & NDK Tools*.
- `adb`, fourni avec le SDK Android d'Unity, généralement sous
  `...\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\`.

**Sur chaque casque**

- Mode développeur activé, via l'application mobile Meta Horizon. Un compte
  développeur est nécessaire, il est gratuit.
- Un câble USB-C pour la première installation.

---

## 2. Préparer le projet (une seule fois)

Le projet est actuellement configuré pour **Windows**. Il faut le basculer sur
Android.

1. *File → Build Profiles*, sélectionner **Android**, puis **Switch Platform**.
   Le réimport complet prend une dizaine de minutes.
2. Vérifier que `Assets/Scenes/SampleScene.unity` est bien la seule scène
   cochée dans la liste.
3. *Project Settings → Player → Android* :
   - **Scripting Backend** : IL2CPP *(déjà configuré)*
   - **Target Architectures** : ARM64 uniquement *(déjà configuré)*
   - **Minimum API Level** : 34 *(déjà configuré)*
   - **Package Name** : remplacer `com.DefaultCompany.VRTemplate` par votre
     propre identifiant, par exemple `com.votreetablissement.tableauvr`. La
     valeur par défaut fonctionne, mais entre en collision avec tout autre
     projet issu du même template Unity.
4. *Project Settings → XR Plug-in Management → Android* : **OpenXR** doit être
   coché, avec le profil de contrôleurs correspondant à vos casques
   (*Meta Quest Touch Plus Controller Profile* pour les Quest 3).

---

## 3. Produire le build

*File → Build Profiles → Build*, puis enregistrer sous un nom explicite, par
exemple `TableauVR-v1.apk`.

Le même APK sert d'hôte et de client : le rôle se choisit au lancement, pas à
la compilation.

---

## 4. Installer sur les casques

Casque branché en USB, autorisation de débogage acceptée dans le casque :

```
adb devices                          # le casque doit apparaître en "device"
adb install -r TableauVR-v1.apk      # -r pour réinstaller par-dessus
```

Répéter pour le second casque. L'application apparaît dans la bibliothèque du
casque, section **Sources inconnues**.

---

## 5. Lancer une session

Les deux casques doivent être **sur le même réseau Wi-Fi**.

**Sur le premier casque — l'hôte**

1. Lancer l'application.
2. Appuyer sur **Start Host** dans le panneau affiché.

Ce casque devient serveur *et* participant : il dessine comme les autres et
conserve l'historique complet des traits.

**Sur le second casque — le client**

1. Lancer l'application.
2. Appuyer sur **Find Servers**. La recherche émet un broadcast UDP toutes les
   3 secondes.
3. L'hôte apparaît dans la liste au bout de quelques secondes. Appuyer dessus
   pour le rejoindre. *(à valider)*

Une fois connecté, le client reçoit automatiquement tout ce qui a déjà été
dessiné, par lots de 20 paquets. Un participant qui arrive en cours de séance
retrouve donc le tableau dans son état courant.

**Variante : le PC comme hôte.** Ouvrir le projet dans Unity, entrer en Play
mode, cliquer **Start Host**. Les casques rejoignent de la même façon. Utile
pour garder le tableau vivant même quand tous les casques sont reposés, et pour
suivre la séance depuis l'écran.

**Pour dessiner**, approcher la pointe du marqueur à moins de 3 cm de la
surface du tableau. Le marqueur suit la main droite. *(à valider)*

---

## 6. Réseau : les trois pièges qui font échouer une séance

**L'isolation de points d'accès.** C'est le problème numéro un. La plupart des
Wi-Fi d'établissement isolent les clients les uns des autres : chaque appareil
atteint Internet, mais aucun ne voit ses voisins. Le broadcast de découverte ne
passe alors pas, et une connexion par IP directe échoue également. **Prévoyez
un routeur dédié** — un simple routeur grand public, non relié au réseau de
l'établissement, suffit. C'est du LAN pur : aucun accès Internet n'est
nécessaire au fonctionnement.

**Les deux ports UDP.** Il en faut deux, pas un :

| Port | Rôle |
|------|------|
| **UDP 7777** | Transport KCP — le trafic de jeu proprement dit |
| **UDP 47777** | Broadcast de découverte des serveurs |

Si le 47777 est bloqué, **Find Servers** ne trouvera jamais rien alors même que
tout le reste est correct.

**Le pare-feu Windows**, uniquement si le PC fait office d'hôte. Autoriser
Unity, ou l'exécutable du build, sur ces deux ports UDP et **en profil réseau
privé**. Windows classe souvent un réseau nouvellement rencontré en « public »
et bloque tout en silence : vérifiez le profil du réseau avant d'accuser le
code.

---

## 7. Valider d'abord sur PC

Avant de mobiliser deux casques et un groupe, faites tourner la séance au
clavier. C'est là que se diagnostiquent tous les problèmes de logique.

1. Produire une version Windows du projet.
2. Lancer l'exécutable, cliquer **Start Host**.
3. Lancer une seconde instance, cliquer **Find Servers**, puis rejoindre.

Tout ce qui relève du réseau — découverte, partage des traits, rattrapage d'un
arrivant tardif — se valide intégralement ainsi, sur une seule machine. Les
casques ne servent ensuite qu'à éprouver l'ergonomie et le réseau physique.

---

## 8. Dépannage

**« Find Servers » ne trouve rien.**
Dans l'ordre : les deux appareils sont-ils sur le même réseau ? Le Wi-Fi
isole-t-il ses clients (section 6) ? Le port UDP 47777 est-il ouvert ? L'hôte
a-t-il réellement démarré — le bouton doit être passé sur **Stop Host** ?

**Le client trouve le serveur mais la connexion échoue.**
La découverte passe (47777) mais pas le transport : c'est le port UDP 7777 qui
est bloqué.

**Le tableau reste blanc alors que quelqu'un dessine.**
Vérifier que le marqueur touche bien la surface, la portée est de 3 cm. Si
personne ne parvient à tracer, le problème est dans le rendu local et non dans
le réseau : testez en solo, hors connexion, pour trancher.

**Les traits apparaissent chez l'hôte mais pas chez le client.**
C'est un problème de réplication. Consulter la console : Mirror signale les
messages rejetés.

**L'application se ferme au lancement sur le casque.**
Presque toujours une erreur de configuration de build : architecture autre
qu'ARM64, ou OpenXR non activé pour Android (section 2).

**Récupérer les logs d'un casque :**

```
adb logcat -s Unity:V
```

---

## 9. Limites connues

- **Pas d'avatars.** Vous voyez le marqueur des autres participants, pas leur
  corps ni leur visage.
- **Une seule couleur et une seule épaisseur**, fixées dans le prefab. Ni
  palette ni sélecteur en VR pour l'instant.
- **Pas de gomme.**
- **L'annulation efface un trait entier** et rejoue tout l'historique. Sur une
  séance très longue, cette opération devient coûteuse.
- **L'historique n'est pas sauvegardé.** Quand l'hôte quitte, le tableau est
  perdu.
- Dimensionné pour une dizaine de participants simultanés.
