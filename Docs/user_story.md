## Szakdolgozat: Keresőalgoritmusok  interaktív  alkalmazása  és állapotgráfok ábrázolása.
#### Szécsi Szabolcs Ádám T2HLYG

# USER STORY


| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Kiválasztani egy pályát |
| GIVEN: | A feladvány választó fül meg van nyitva |
| WHEN: | Rányomok a megfelelő pálya előnézet rublikájára |
| THEN: | Betöltődik a pálya kezőállapota, a állapotgráfon csak a kezdőállapot látszik, átvált a jobb panel az algoritmus beállítások fülre |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Felfedezni az állapotgráf csúcsait |
| GIVEN: | Kereső algoritmus beállítások fül meg van nyitva |
| WHEN: | Rányomok a csúcsfelfedezés gombjára |
| THEN: | Az állapotgráfon felvesszük a felfedezett csúcsokat, a célcsúcsok ki vannak emelve |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Megállítani a csúcsfelfedezést |
| GIVEN: | Épp fut csúcsfelfedezés |
| WHEN: | Rányomok a csúcsfelfedezés megállítása gombjára |
| THEN: | A csúcsfelfedezés megáll |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Kiválasztani milyen keresőalgoritmust legyen kiválasztva |
| GIVEN: | Kereső algoritmus beállítások fül meg van nyitva |
| WHEN: | Kiválasztom a megfelelő keresőalgoritmust az opciók közül |
| THEN: | A keresőalgoritmus beállításra kerül |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Kiválasztani milyen heurisztikus függvény legyen kiválasztva |
| GIVEN: | Kereső algoritmus beállítások fül meg van nyitva |
| WHEN: | Kiválasztom a megfelelő heurisztikus függvényt az opciók közül |
| THEN: | A heurisztika beállításra kerül |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Futtatni a beállított keresőalgoritmust |
| GIVEN: | Kereső algoritmus beállítások fül meg van nyitva |
| WHEN: | Rányomok a keresőalgoritmus alkalmazása gombra |
| THEN: | A kiválasztott beállítások alapján alkalmazva lesz a kereső algoritmus a betöltött feladványra |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Elindítani/folytatni a keresőalgoritmust |
| GIVEN: | A játéktábla fül meg van nyitva |
| WHEN: | Rányomok a keresőalgoritmus indítása/folytatása gombjára |
| THEN: | A keresőalgoritmus elindul, a gráfon ki vannak emelve a globális munkaterületen tárolt csúcsok és élek, ha a keresőalgoritmus talál célállapotot, akkor leáll az algoritmus |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Megállítani a keresőalgoritmust |
| GIVEN: | A játéktábla fül meg van nyitva és épp fut keresőalgoritmus |
| WHEN: | Rányomok a keresőalgoritmus megállítása gombjára |
| THEN: | A keresőalgoritmus megáll, a gráfon a globális munkaterületen tárolt csúcsok és élek ki vannak emelve |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Lépésenként futtatni a keresőalgoritmust |
| GIVEN: | A játéktábla fül meg van nyitva |
| WHEN: | Rányomok a keresőalgoritmus lépésenkénti futtatása gombjára |
| THEN: | A keresőalgoritmus egy lépést futtat, a gráfon ki vannak emelve a globális munkaterületen tárolt csúcsok és élek |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Beállítani a keresőalgoritmus sebességét |
| GIVEN: | A játéktábla fül meg van nyitva |
| WHEN: | A sebesség állító csúszkát a megfelelő helyre húzom |
| THEN: | A keresőalgoritmus minden lépése a beállított sebességgel történik |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Manuálisan lépni a táblán |
| GIVEN: | A játéktábla fül meg van nyitva |
| WHEN: | A játéktáblán kattintok a lépésnek megfelelő piros nyílra |
| THEN: | A játéktáblán elmozdul az autó, az állapotgráfon felfedezzük, kitüntetjük az új állapot csúcsát |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | A játéktáblát manuális módba váltani |
| GIVEN: | A játéktábla fül meg van nyitva |
| WHEN: | A játéktábla fölötti manuális gombra kattintok |
| THEN: | A játéktábla manuális módba vált, az állapotgráfon ki van tüntetve a megjelenített állapot, ha futott algoritmus az a háttérben fut tovább |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Az algoritmus lépéseit megjeleníteni a táblán |
| GIVEN: | A játéktábla fül meg van nyitva |
| WHEN: | A játéktábla fölötti algoritmus gombra kattintok |
| THEN: | A játéktábla algoritmus módba vált, a táblán az algoritmus által legutóbb kiterjesztett állapot van megjelenítve, a játéktábla mutatja az algoritmus újjab kiterjesztett állapotait |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Egy felfedezett állapotba ugrani |
| GIVEN: | Egy feladvány be van töltve |
| WHEN: | Rányomok (M1) egy felfedezett állapot csúcsára az állapotgráfon |
| THEN: | A játéktábla a kiválasztott állapotot mutatja, a gráfon a kiválasztott csúcs ki van emelve |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Forgarni az állapotgráfot |
| GIVEN: | Egy feladvány be van töltve |
| WHEN: | Az állapotgráfon kattintok (M2) és húzom az egeret |
| THEN: | Az állapotgráf forog az aktív csúcs körül az egér mozgásának megfelelően |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Mozgatni az állapotgráfot |
| GIVEN: | Egy feladvány be van töltve |
| WHEN: | A WASD gombok közül nyomom valamelyiket  |
| THEN: | Az állapotgráf elmozdul a vásznon a gombnak megfelelően |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Az állapotgráfon elrejteni/megjeleníteni a nem releváns éleket, csúcsokat |
| GIVEN: | A játéktábla fül meg van nyitva |
| WHEN: | Rákattintok az irreleváns élek és csúcsok elrejtésének kapcsolójára |
| THEN: | Az állapotgráfon elrejtődnek/megjelennek az irreleváns élek, csúcsok |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Követni az állapotgráfon az algoritmus által kiterjesztett csúcsokat |
| GIVEN: | A játéktábla fül meg van nyitva |
| WHEN: | Rákattintok az algoritmus csúcsai követésének kapcsolójára |
| THEN: | Az állapotgráf kamerája követi az algoritmus által kiterjesztett csúcsokat |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Nagyítani/Kicsinyíteni az állapotgráfot |
| GIVEN: | Egy feladvány be van töltve |
| WHEN: | Az állapotgráfon az egér görgőjét használom |
| THEN: | Az állapotgráf nagyítódik/kicsinyítódik az egér görgőjének megfelelően |

| AS A: | Felhasználó |
| --- | --- |
| I WANT TO: | Kilépni a programból |
| GIVEN: | A program fut |
| WHEN: | Bezárom a program ablakát |
| THEN: | A program bezárul |
