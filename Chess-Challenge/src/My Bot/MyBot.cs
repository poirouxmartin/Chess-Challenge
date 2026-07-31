using ChessChallenge.API;
using static System.Math;


// TODO

// benchmarks -> nodes per second
// check the number of evaluated positions and check if move ordering and pruning are working
// undo faster than board copy?
// time management
// use bitboards for attacking pieces
// transposition table

// EVALUATION
// incremental evaluation
// king safety
// piece activity or placement
// endgame evaluation

// ALGORITHMS
// iterative deepening
// quiescence further pruning (delta + null move)
// killer move



public class MyBot : IChessBot
{

    // Evaluation
    int[] pieceValues = { 0, 80, 320, 350, 480, 950, 10000 };
    int kingPlacementFactor = 50;

    // Move to play
    Move bestMove;

    // Search depth
    int maxDepth = 6;
    int quiescenceDepth = 8;
    int nullDepth = 2;

    // Constants
    int evalMax = 1000000;


    public Move Think(Board board, Timer timer)
    {
        System.Console.WriteLine("Timer: " + timer.MillisecondsRemaining + ", Current evaluation: " + Evaluate(board));
        Negamax(board, maxDepth, -evalMax, evalMax, true, nullDepth);
        return bestMove;
    }
    
    public int Color(Board board)
    {
        return board.IsWhiteToMove ? 1 : -1;
    }

    // Simple evaluation function
    int Evaluate(Board board)
    {
        int evaluation = 0;

        // TODO : possibility to use piece lists instead of board
        // Piece values
        int materialWhite = 0;
        int materialBlack = 0;
        for (int i = 0; i < 64; i++)
        {
            Square square = new(i);
            Piece piece = board.GetPiece(square);
            if (piece.IsWhite)
                materialWhite += pieceValues[(int)piece.PieceType];
            else
                materialBlack += pieceValues[(int)piece.PieceType];
        }
        

        evaluation += materialWhite - materialBlack;

        // King placement (better on the edge in middle game)
        // TODO : better in the middle in end game
        Square whiteKing = board.GetKingSquare(true);
        evaluation -= kingPlacementFactor * (Min(whiteKing.File, 7 - whiteKing.File) + Min(whiteKing.Rank, 7 - whiteKing.Rank));

        Square blackKing = board.GetKingSquare(false);
        evaluation += kingPlacementFactor * (Min(blackKing.File, 7 - blackKing.File) + Min(blackKing.Rank, 7 - blackKing.Rank));

        // Favors attacks towards enemy king
        //board.

        // Player trait
        evaluation += 20 * Color(board);

        return evaluation;
    }

    // Incremental evaluation
    //int UpdateEvaluation(Board board, Move move)
    //{
    //    int evaluation = 0;

    //    // Piece values
    //    Piece piece = board.GetPiece(move.StartSquare);
    //    evaluation += Color(board) * pieceValues[(int)piece.PieceType];

    //    // King placement (better on the edge in middle game)
    //    // TODO : better in the middle in end game
    //    Square king = board.GetKingSquare(piece.IsWhite);
    //    evaluation -= Color(board) * kingPlacementFactor * (Min(king.File, 7 - king.File) + Min(king.Rank, 7 - king.Rank));

    //    // Player trait
    //    evaluation += 40 * Color(board);

    //    return evaluation;
    //}


    int Negamax(Board board, int depth, int alpha, int beta, bool root, int nullDepth)
    {
        // Terminal node
        if (board.IsInCheckmate())
            return -10000 * (depth - 1);
        if (board.IsDraw())
            return 0;

        // Quiescence search
        if (depth <= 0)
            return Quiescence(board, quiescenceDepth, -evalMax, evalMax);

        // Null move pruning
        if (nullDepth > 0 && depth > 1 && !board.IsInCheck())
        {
            board.TrySkipTurn(); // Perform the null move
            int nullMoveScore = -Negamax(board, depth - 1 - nullDepth, -beta, -beta + 1, false, 0);
            board.UndoSkipTurn(); // Undo the null move

            if (nullMoveScore >= beta)
                return nullMoveScore; // Cut-off the search early
        }


        // Best move value
        int max = -100000;

        // Sorts moves by evaluation
        Move[] moves = SortedMoves(board);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int score = -Negamax(board, depth - 1, -beta, -alpha, false, nullDepth);
            //if (root)
            //    System.Console.WriteLine(move + ": " + score);
            board.UndoMove(move);

            if (root && score > max)
                bestMove = move;

            max = Max(max, score);
            alpha = Max(alpha, score);

            if (alpha >= beta)
                break;
        }

        return max;
    }


    int Quiescence(Board board, int depth, int alpha, int beta)
    {
        if (board.IsInCheckmate())
            return -10000 * (depth - 1);
        if (board.IsDraw())
            return 0;

        if (board.IsInCheck())
            depth++;
        int standPat = Color(board) * Evaluate(board);

        if (depth == 0)
            return standPat;

        if (standPat >= beta)
            return beta;

        if (alpha < standPat)
            alpha = standPat;

        //Move[] moves = board.GetLegalMoves(true);
        Move[] moves = SortedCaptures(board);
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int score = -Quiescence(board, depth - 1, -beta, -alpha);
            board.UndoMove(move);

            if (score >= beta)
                return beta;

            if (score > alpha)
                alpha = score;
        }

        return alpha;
    }

    Move[] SortedMoves(Board board)
    {

        // Gets the value of each move
        Move[] moves = board.GetLegalMoves();
        int[] scores = new int[moves.Length];
        int i = 0;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            scores[i] = -Color(board) * Evaluate(board);
            board.UndoMove(move);
            i++;
        }

        int[] move_indices = new int[moves.Length];
        for (int j = 0; j < moves.Length; j++)
        {
            int max_index = 0;
            for (int k = 0; k < moves.Length; k++)
                if (scores[k] > scores[max_index])
                    max_index = k;
            move_indices[j] = max_index;
            scores[max_index] = -evalMax;
        }

        Move[] sortedMoves = new Move[moves.Length];
        for (int j = 0; j < moves.Length; j++)
            sortedMoves[j] = moves[move_indices[j]];

        return sortedMoves;
    }


    Move[] SortedCaptures(Board board)
    {
        // Gets the value of each move
        Move[] moves = board.GetLegalMoves(true);
        int[] scores = new int[moves.Length];
        int i = 0;

        foreach (Move move in moves)
        {
            Piece capturerPiece = board.GetPiece(move.StartSquare);
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            scores[i] = pieceValues[(int)capturedPiece.PieceType] / pieceValues[(int)capturerPiece.PieceType];
            i++;
        }

        int[] move_indices = new int[moves.Length];
        for (int j = 0; j < moves.Length; j++)
        {
            int max_index = 0;
            for (int k = 0; k < moves.Length; k++)
                if (scores[k] > scores[max_index])
                    max_index = k;
            move_indices[j] = max_index;
            scores[max_index] = -evalMax;
        }

        Move[] sortedMoves = new Move[moves.Length];
        for (int j = 0; j < moves.Length; j++)
            sortedMoves[j] = moves[move_indices[j]];

        return sortedMoves;
    }

    int IterativeDeepening(Board board)
    {
        return 0;
    }


}
