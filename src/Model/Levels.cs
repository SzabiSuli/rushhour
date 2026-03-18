namespace rushhour.src.Model;

using System;
using System.Collections.Generic;
using Godot;

static class Levels {
    // return the title, and the state
    public static (string, RHGameState) LoadLevel(int index) {
        if (index < 0 || index >= levelStrings.Length) {
            throw new ArgumentException($"Level index must be between 0 and {levelStrings.Length}");
        }
        string[] lvlS = levelStrings[index].Split("\n");
        if (lvlS.Length != 7) {
            throw new Exception($"Level {index} is not formatted correctly.");
        }
        string title = lvlS[0];

        List<PlacedRHPiece> vehicles = new();

        for (int i = 1; i <= 6; i++) {
            string row = lvlS[i];
            for (int j = 0; j < 6; j++) {
                char c = row[j];
                
                // keep going until we find a capital letter
                if (Char.IsUpper(c)) {
                    PlacedRHPiece pp = GetPiece(lvlS, c, i, j);
                    if (pp.Piece is MainCar) {
                        // MainCar must be inserted to index 0
                        vehicles.Insert(0, pp);
                    } else {
                        vehicles.Add(pp);
                    }
                }
            }
        }

        return (title, new RHGameState(vehicles.ToArray()));
    }

    public static PlacedRHPiece GetPiece(string[] lvlS, char c, int i, int j) {
        if (lvlS[i][j] != c) {
            throw new ArgumentException($"The front of car labeled with {c} is not at cooridinates {i}, {j}.");
        }
        char lower = Char.ToLower(c);

        // find its neighbouring cell
        foreach (Direction d in Enum.GetValues<Direction>()) {
            if (GetChar(lvlS, i, j, d, 1) == lower) {
                RushHourPiece piece;
                if (GetChar(lvlS, i, j, d, 2) == lower) {
                    // we found a bus
                    piece = new Bus();
                } else {
                    // we found a car
                    if (c == 'A') {
                        piece = new MainCar();    
                    } else {
                        piece = new Car();
                    }
                }

                return new PlacedRHPiece(piece, new Vector2I(j, i - 1), d.GetOpposite());
            }
        }

        throw new Exception($"No neighbouring cell for {c} at ({i},{j}) contains its lower-case body.");
    }

    public static char? GetChar(string[] lvlS, int i, int j, Direction d, int tiles = 1) {
        
        int[,] directions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };

        int ni = i + directions[(int)d, 0] * tiles;
        int nj = j + directions[(int)d, 1] * tiles;

        if (ni < 1 || ni > 6 || nj < 0 || nj > 5){
            return null;
        }
        return lvlS[ni][nj];
    }

    // Capital letter marks the front of the vehicle
    public static readonly string[] levelStrings = [
        """
        Template
        aA....
        ......
        ......
        ......
        ......
        ......
        """,
        """
        Square
        aA....
        bB....
        ......
        ......
        ......
        ......
        """,
        """
        Cube
        ......
        aA....
        ......
        bB....
        cC....
        ......
        """,
        """
        Level 1
        bB...C
        e..h.c
        eaAh.c
        E..H..
        F...Dd
        f.ggG.
        """,
        """
        Level 8
        ...nNm
        ..jJkm
        aAhiKM
        bBHIlL
        cCGeeE
        dDgffF
        """,
        """
        Level ? Easy
        ......
        ..E.f.
        aAe.fg
        bbB.FG
        CD..hH
        cd..iI
        """,
        """
        Level ?
        ......
        ..E.f.
        aAe.fg
        bbB.FG
        CDJ.hH
        cdj.iI
        """,
        // index 7
        """
        Very easy
        ...b..
        ...b..
        aA.B..
        ..D...
        E.dCcc
        e...fF
        """
    ];
}
