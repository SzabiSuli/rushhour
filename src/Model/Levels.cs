namespace rushhour.src.Model;

using Godot;

static class Levels {
    
    public static RHGameState TestLevel() {
        return new RHGameState([
            // Index 0: Red main car — horizontal, row 2, cols 0–1 (front at col 1, facing right, body extends left)
            new PlacedRHPiece(new MainCar(), new Vector2I(1, 1), Direction.Right),
            new PlacedRHPiece(new Car(), new Vector2I(1, 3), Direction.Right)
        ]);
    }
    
    /// <summary>
    /// Rush Hour Puzzle #0 (Very Easy)
    /// </summary>
    public static RHGameState Level0() {
        return new RHGameState([
            // Index 0: Red main car — horizontal, row 2, cols 0–1 (front at col 1, facing right, body extends left)
            new PlacedRHPiece(new MainCar(), new Vector2I(1, 2), Direction.Right),

            // Index 1: Light-blue car — vertical, col 2, rows 1–2 (front at row 1, facing up, body extends down)
            new PlacedRHPiece(new Car(), new Vector2I(2, 1), Direction.Up),

            // Index 2: Yellow truck — vertical, col 4, rows 1–3 (front at row 1, facing up, body extends down)
            new PlacedRHPiece(new Bus(), new Vector2I(4, 1), Direction.Up),

            // Index 3: Orange car — vertical, col 5, rows 2–3 (front at row 2, facing up, body extends down)
            new PlacedRHPiece(new Car(), new Vector2I(5, 2), Direction.Up),

            // Index 4: Lavender truck — horizontal, row 3, cols 0–2 (front at col 0, facing left, body extends right)
            new PlacedRHPiece(new Bus(), new Vector2I(0, 3), Direction.Left),

            // Index 5: Blue car — vertical, col 0, rows 4–5 (front at row 4, facing up, body extends down)
            new PlacedRHPiece(new Car(), new Vector2I(0, 4), Direction.Up),

            // Index 6: Pink car — vertical, col 1, rows 4–5 (front at row 4, facing up, body extends down)
            new PlacedRHPiece(new Car(), new Vector2I(1, 4), Direction.Up),

            // Index 7: Dark purple car — vertical, col 2, rows 4–5 (front at row 4, facing up, body extends down)
            // new PlacedRHPiece(new Car(), new Vector2I(2, 4), Direction.Up),

            // Index 8: Teal car — horizontal, row 4, cols 4–5 (front at col 4, facing left, body extends right)
            new PlacedRHPiece(new Car(), new Vector2I(4, 4), Direction.Left),

            // Index 9: Gray car — horizontal, row 5, cols 4–5 (front at col 4, facing left, body extends right)
            new PlacedRHPiece(new Car(), new Vector2I(4, 5), Direction.Left),
        ]);
    }
    /// <summary>
    /// Rush Hour Puzzle #1 (Beginner)
    /// </summary>
    public static RHGameState Level1() {
        return new RHGameState([
            // Index 0: Red main car — horizontal, row 2, cols 0–1 (front at col 1, facing right, body extends left)
            new PlacedRHPiece(new MainCar(), new Vector2I(1, 2), Direction.Right),

            // Index 1: Light-blue car — vertical, col 2, rows 1–2 (front at row 1, facing up, body extends down)
            new PlacedRHPiece(new Car(), new Vector2I(2, 1), Direction.Up),

            // Index 2: Yellow truck — vertical, col 4, rows 1–3 (front at row 1, facing up, body extends down)
            new PlacedRHPiece(new Bus(), new Vector2I(4, 1), Direction.Up),

            // Index 3: Orange car — vertical, col 5, rows 2–3 (front at row 2, facing up, body extends down)
            new PlacedRHPiece(new Car(), new Vector2I(5, 2), Direction.Up),

            // Index 4: Lavender truck — horizontal, row 3, cols 0–2 (front at col 0, facing left, body extends right)
            new PlacedRHPiece(new Bus(), new Vector2I(0, 3), Direction.Left),

            // Index 5: Blue car — vertical, col 0, rows 4–5 (front at row 4, facing up, body extends down)
            new PlacedRHPiece(new Car(), new Vector2I(0, 4), Direction.Up),

            // Index 6: Pink car — vertical, col 1, rows 4–5 (front at row 4, facing up, body extends down)
            new PlacedRHPiece(new Car(), new Vector2I(1, 4), Direction.Up),

            // Index 7: Dark purple car — vertical, col 2, rows 4–5 (front at row 4, facing up, body extends down)
            new PlacedRHPiece(new Car(), new Vector2I(2, 4), Direction.Up),

            // Index 8: Teal car — horizontal, row 4, cols 4–5 (front at col 4, facing left, body extends right)
            new PlacedRHPiece(new Car(), new Vector2I(4, 4), Direction.Left),

            // Index 9: Gray car — horizontal, row 5, cols 4–5 (front at col 4, facing left, body extends right)
            new PlacedRHPiece(new Car(), new Vector2I(4, 5), Direction.Left),
        ]);
    }
}
