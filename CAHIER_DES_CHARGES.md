# Cahier des charges — Visite immersive collaborative « Résidence Les Tilleuls »

**Client :** Atelier Habitat & Co., promoteur immobilier *(société fictive)*
**Interlocutrice :** Claire Delorme, responsable commerciale
**Destinataire :** le prestataire chargé de réaliser l'application
**Version :** 1.0 — `[À COMPLÉTER : date]`

---

## 1. Qui sommes-nous

Atelier Habitat & Co. construit et commercialise des logements neufs de taille
moyenne en centre-ville. Nous vendons la quasi-totalité de nos appartements
**sur plan**, c'est-à-dire avant même que le premier mur soit monté.

Notre prochain programme, la **Résidence Les Tilleuls**, est un petit immeuble
de 5 niveaux situé à Nantes. La commercialisation démarre dans quelques mois,
depuis notre espace de vente installé au pied du chantier.

## 2. Notre problème

Aujourd'hui, un acheteur choisit un appartement à partir d'un plan en 2D, de
quelques images de synthèse et d'une plaquette. Nous constatons trois
difficultés récurrentes :

1. **Les clients ont du mal à se projeter.** Un plan ne dit pas si la chambre
   « fait petit », si la cuisine est sombre, ou si l'on voit le parc depuis le
   séjour.
2. **La lumière est la grande inconnue.** « Est-ce que le séjour aura du soleil
   l'après-midi ? » est la question que l'on nous pose le plus, et nous y
   répondons avec une boussole dessinée sur le plan.
3. **On achète rarement seul.** Un couple, des parents qui accompagnent leur
   enfant, un architecte d'intérieur… Les décisions se prennent à plusieurs, et
   les discussions autour d'un plan papier sont peu productives.

## 3. Ce que nous voulons

Une application de **visite en réalité virtuelle** qui permette à plusieurs
personnes, chacune équipée d'un casque, de se retrouver **ensemble** dans
l'appartement avant sa construction. On doit pouvoir s'y promener, voir la
lumière du jour évoluer, noter des idées et essayer des aménagements.

Une visite typique réunit **2 à 4 personnes** : en général le ou les acheteurs
et un commercial de notre équipe.

---

## 4. Le bien à présenter

### 4.1 L'immeuble

- Résidence de **5 niveaux** (rez-de-chaussée + 4 étages), toiture terrasse.
- **2 appartements par étage.**
- Façade principale orientée **sud**, côté rue. Un petit parc borde l'immeuble
  à l'**ouest**.
- **Vis-à-vis :** un immeuble existant de 7 niveaux se trouve à environ 20 m
  côté **est**. Nos clients s'inquiètent souvent de l'ombre qu'il projette.
- Localisation : Nantes (latitude ≈ 47,2° N).

### 4.2 L'appartement témoin : T3 du 3e étage (lot B31)

Surface habitable : **65 m²**, plus un **balcon de 8 m²** orienté sud-ouest.
Hauteur sous plafond : 2,50 m.

| Pièce | Dimensions indicatives | Surface | Ouverture(s) |
|---|---|---|---|
| Séjour + cuisine ouverte | 6,0 × 4,5 m | 27,0 m² | Baie vitrée **sud** sur le balcon, fenêtre **ouest** |
| Chambre 1 (parentale) | 3,5 × 3,6 m | 12,6 m² | Fenêtre **est** |
| Chambre 2 | 3,0 × 3,4 m | 10,2 m² | Fenêtre **est** |
| Salle de bain | 2,4 × 2,2 m | 5,3 m² | Petite fenêtre **nord** |
| WC séparé | 1,0 × 1,5 m | 1,5 m² | Aucune |
| Entrée + dégagement | — | 8,4 m² | Porte palière **nord** |
| **Total** | | **65,0 m²** | |

Croquis de principe (non à l'échelle) :

```
                         NORD  (palier de l'immeuble)
   +-------------+==porte==+---------+----+--------------+
   |             |         |         |    |              |
   |  Cuisine    | Entrée  |   SdB   | WC |  Chambre 1   ]
   |  ouverte    |         |         |    |              ]
   |             +--  -----+---------+----+-----  -------+
OUEST                  dégagement                         EST
   [                   +-------------------------  ------+
   [   Séjour          |                     |           ]
   |                   |    (placards)       | Chambre 2 ]
   |                   |                     |           |
   +=====baie vitrée===+---------------------+-----------+
    ~~~~~~ balcon 8 m² ~~~~~~
                          SUD  (rue)
```

> Ce plan est fourni à titre indicatif. **Un plan coté définitif vous sera
> remis** `[À COMPLÉTER : plan de l'enseignant]` ; il fait foi en cas d'écart.

---

## 5. Besoins fonctionnels

Chaque besoin porte un identifiant (F1 à F8) que nous réutiliserons lors de la
recette.

### F1 — Se promener dans l'appartement
Le visiteur doit se déplacer librement dans l'appartement **à taille réelle** :
passer d'une pièce à l'autre, s'approcher d'une fenêtre, sortir sur le balcon.
Nos clients ne sont pas des joueurs de jeux vidéo : le déplacement doit être
simple, et **personne ne doit avoir mal au cœur**.

### F2 — Visiter ensemble
Toutes les personnes présentes sont dans le **même appartement, au même
moment**. Chacun voit où se trouvent les autres et vers où ils regardent, pour
pouvoir dire « viens voir ici ». Ce que l'un modifie (couleur, meuble,
heure…) doit être vu **par tout le monde**.

### F3 — Savoir où est le nord
À tout moment, le visiteur doit pouvoir savoir comment la pièce où il se
trouve est orientée.

### F4 — Voir la lumière selon l'heure
Nous voulons pouvoir choisir un **moment de la journée**, du lever au coucher
du soleil, et voir comment la lumière naturelle entre dans chaque pièce, ombre
de l'immeuble voisin comprise. Idéalement, on pourrait aussi comparer
**l'hiver et l'été**.

### F5 — Un tableau pour noter les idées
Pendant la visite, les participants doivent pouvoir **écrire ou dessiner** sur
un tableau partagé : questions pour le notaire, idées de travaux, croquis de
meubles…

### F6 — Aménager en écrivant
C'est la fonction qui ferait la différence face à nos concurrents. Quand un
visiteur **écrit le nom d'un équipement** sur le tableau, cet équipement
**apparaît dans l'appartement**, au bon endroit.

Exemple : dans la salle de bain, j'écris *baignoire* et une baignoire
apparaît ; j'écris *douche* et la baignoire est remplacée par une douche à
l'italienne. La liste des mots à reconnaître est en annexe B.

### F7 — Changer la couleur des murs
Le visiteur choisit une couleur parmi notre palette (annexe C) et l'applique
aux murs d'une pièce. En option, il pourrait aussi changer le revêtement de
sol (parquet clair, parquet foncé, carrelage).

### F8 — Voir l'immeuble de l'extérieur, comme une maquette
Nous voulons pouvoir **sortir de l'appartement** et voir la résidence dans
son quartier. Surtout, le visiteur doit pouvoir **passer en vue maquette** :
l'immeuble devient une maquette posée devant lui, dont il peut faire le tour
pour comprendre où se situe son appartement (étage, orientation, vis-à-vis).
Il doit ensuite pouvoir **revenir dans l'appartement**.

---

## 6. Contraintes

| Contrainte | Détail |
|---|---|
| **Pas d'Internet** | L'espace de vente n'a pas de connexion fiable. Tout doit fonctionner sur **notre propre réseau Wi-Fi local**. |
| **Aucun abonnement** | Nous refusons tout service en ligne payant, tout compte à créer ou toute licence renouvelable. |
| **Matériel** | Casques autonomes **Meta Quest** et un PC portable dans l'espace de vente. |
| **Prise en main** | Un acheteur qui n'a jamais mis de casque doit être autonome en **moins de 2 minutes**, avec les explications du commercial. |
| **Confort** | Des visites de 15 à 20 minutes sans fatigue ni nausée. |
| **Image de marque** | Rendu sobre et soigné, crédible pour un logement neuf. Nous préférons la simplicité maîtrisée à la surcharge. |

---

## 7. Nos priorités

Si tout ne peut pas être livré, voici ce qui compte pour nous :

| Priorité | Besoins |
|---|---|
| **Indispensable** | F1 Promenade · F2 Visite à plusieurs · F3 Nord · F4 Lumière selon l'heure · F5 Tableau · F7 Couleur des murs |
| **Très souhaité** | F6 Aménager en écrivant · F8 Vue maquette |
| **Appréciable** | Comparaison hiver / été · Revêtements de sol · Retrouver l'aménagement choisi lors d'une prochaine visite |
| **Hors périmètre** | Estimation de prix ou devis · Visite à distance par Internet · Catalogue complet de mobilier |

---

## 8. Comment nous validerons la livraison (recette)

Lors de la présentation, un membre de notre équipe jouera l'acheteur. Nous
déroulerons les scénarios ci-dessous avec **au moins deux casques**.

| # | Scénario | Besoin |
|---|---|---|
| R1 | Deux visiteurs arrivent dans l'entrée. Chacun voit l'autre. | F1, F2 |
| R2 | Le premier visiteur va dans la chambre 1 ; le second le rejoint sans aide. | F1, F2 |
| R3 | Dans le séjour, un visiteur indique où se trouve le nord. | F3 |
| R4 | On règle l'heure sur 16 h : le soleil entre par la baie du séjour. On passe à 8 h : il entre dans les chambres, et l'ombre de l'immeuble voisin est visible. Les deux visiteurs voient la même chose. | F4, F2 |
| R5 | Un visiteur écrit « prévoir prises USB » sur le tableau ; l'autre le lit. | F5 |
| R6 | Dans la salle de bain, un visiteur écrit *baignoire* : une baignoire apparaît. Il écrit *douche* : elle est remplacée. L'autre visiteur voit les changements. | F6 |
| R7 | Un visiteur peint le séjour en vert sauge ; l'autre voit la nouvelle couleur. | F7 |
| R8 | Un visiteur passe en vue maquette, montre à l'autre l'emplacement du lot B31, puis ils reviennent tous deux dans l'appartement. | F8 |
| R9 | Un troisième visiteur rejoint une visite déjà en cours : il retrouve les couleurs, les équipements et le contenu du tableau. | F2 |

---

## Annexe A — Plan de l'appartement

Voir section 4.2. Plan coté définitif : `[À COMPLÉTER]`.

## Annexe B — Vocabulaire à reconnaître sur le tableau (F6)

Chaque mot correspond à un équipement placé à un **emplacement prévu** de la
pièce. Deux mots d'un même emplacement s'excluent : écrire l'un remplace
l'autre.

| Pièce | Emplacement | Mots reconnus |
|---|---|---|
| Salle de bain | Bain / douche | `baignoire` · `douche` |
| Salle de bain | Lavabo | `vasque` · `double vasque` |
| Séjour | Salon | `canapé` · `fauteuils` |
| Séjour | Repas | `table` |
| Cuisine | Plan de travail | `îlot` · `bar` |
| Chambre 1 | Couchage | `lit double` |
| Chambre 2 | Couchage | `lit simple` · `lits superposés` |
| Chambre 2 | Travail | `bureau` |

Au minimum, le couple **baignoire / douche** doit fonctionner.

## Annexe C — Palette de couleurs des murs (F7)

| Nom | Code couleur |
|---|---|
| Blanc cassé *(par défaut)* | `#F2EFE8` |
| Gris perle | `#C9C8C3` |
| Vert sauge | `#A3B18A` |
| Terracotta | `#C8734F` |
| Bleu nuit | `#2E3A59` |
| Jaune ocre | `#D9A441` |
