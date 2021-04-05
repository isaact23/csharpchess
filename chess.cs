using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Chess {

    class Program {
        static void Main(string[] args) {
            ChessSolver chessSolver = new ChessSolver();
            chessSolver.MonteCarloSetup();
        }
    }

    class ChessSolver {

        public void RandomMoves() {
            while (true) {
                Board board = new Board();
                Random random = new Random();
                Move[] moves;
                Move chosen_move;

                bool running = true;
                int counter = 0;

                while (running) {
                    counter += 1;
                    moves = board.ListMoves(0);
                    if (moves.Length == 0) {
                        // Detect checkmate/stalemate here.
                        bool check = false;
                        foreach (Move move in moves) {
                            if (board.piece_data[move.to_x, move.to_y] == "wk" && board.piece_data[move.from_x, move.from_y][0] == 'b') {
                                check = true;
                                running = false;
                                break;
                            }
                        }
                        if (check) {
                            Console.WriteLine("Black wins!");
                            break;
                        } else {
                            Console.WriteLine("Stalemate!");
                            break;
                        }
                    } else if (board.CountKings() == 1) {
                        Console.WriteLine("White wins!");
                        running = false;
                        break;
                    }

                    chosen_move = null; // Pick the best looking move
                    int best_fitness = 0;
                    foreach (Move move in moves) {
                        if (chosen_move == null) {
                            chosen_move = move;
                        } else {
                            Board board_copy = board.Duplicate();
                            board_copy.ApplyMove(move);
                            int fitness = board_copy.Fitness();

                            if (fitness > best_fitness) {
                                chosen_move = move;
                                best_fitness = fitness;
                            }
                        }
                    }
                    //chosen_move = moves[random.Next(moves.Length)];
                    board.ApplyMove(chosen_move);

                    moves = board.ListMoves(1);
                    if (moves.Length == 0) {
                        // Detect checkmate/stalemate here.
                        bool check = false;
                        foreach (Move move in moves) {
                            if (board.piece_data[move.to_x, move.to_y] == "bk" && board.piece_data[move.from_x, move.from_y][0] == 'w') {
                                check = true;
                                running = false;
                                break;
                            }
                        }
                        if (check) {
                            Console.WriteLine("White wins!");
                            break;
                        } else {
                            Console.WriteLine("Stalemate!");
                            break;
                        }
                    } else if (board.CountKings() == 0) {
                        Console.WriteLine("Black wins!");
                        running = false;
                        break;
                    }

                    chosen_move = moves[random.Next(moves.Length)];
                    board.ApplyMove(chosen_move);

                    if (counter > 150) {
                        board.Print();
                    }
                }
            }
        }

        public void PlayMonteCarlo() {
            bool player_white = true;
            Board board = new Board();
            Move[] moves;

            while (true) {
                board.Print();
                Move chosen_move = null;
                moves = board.ListMoves(0);

                while (chosen_move == null) {
                    Console.WriteLine("Input your move: ");

                    string move_name = Console.ReadLine();
                    
                    foreach (Move move in moves) {
                        Board temp_board = board.Duplicate();
                        temp_board.ApplyMove(move);
                        if (move.NameMove(board, temp_board, moves, temp_board.ListMoves(0)) == move_name) {
                            chosen_move = move;
                            break;
                        }
                    }
                }

                board.ApplyMove(chosen_move);
            }
        }

        public void MonteCarloSetup() {


            Console.WriteLine("Determining best line...");

            List<Node> top_nodes = new List<Node>();
            Board board = new Board();
            Random random = new Random();
            Move[] moves = board.ListMoves(0);
            foreach (Move move in moves) {
                top_nodes.Add(new Node(move));
            }

            for (int i = 0; i < 25000; i++) {
                MonteCarlo(board, top_nodes, 2.71f, 0.5f, random);

                Node top_node = null;
                foreach (Node node in top_nodes) {
                    if (top_node == null) {
                        top_node = node;
                    } else if (node.playouts > top_node.playouts) {
                        top_node = node;
                    }
                }
                if ((i + 1) % 10 == 0) {
                    Console.WriteLine(FindBestLine(top_nodes, 10));
                    //Console.WriteLine(top_node.playouts);
                    //Console.WriteLine(top_node.wins);
                }
            }

            Console.WriteLine("Done.");
            Console.ReadKey();
        }

        public class ReturnData { // Used for backpropagation in Monte Carlo
            public float white_wins = 0;
            public float black_wins = 0;
            public int playouts = 0;
        }

        public ReturnData MonteCarlo(Board board, List<Node> parent_nodes, float b, float c, Random random) { // b defaults e, c defaults sqrt 2
            // Formula for node selection: (Wins / Playouts) + c * sqrt ( log b (Total Simulations) / Playouts)

            Board new_board = board.Duplicate();
            Node best_node = null;

            // Calculate the total playouts for the list of nodes given
            int total_playouts = 0;
            foreach (Node node in parent_nodes) {
                total_playouts += node.playouts;

                if (node.playouts == 0) { // If there have been no playouts yet, run them now.
                    Board temp_board = new_board.Duplicate();
                    temp_board.ApplyMove(node.move);
                    int result = temp_board.Playout();
                    node.playouts += 1;

                    if (result == -1) {
                        node.wins += 0.5f;
                    } else if (result == 0 && node.turn == 0) {
                        node.wins += 1;
                    } else if (result == 1 && node.turn == 1) {
                        node.wins += 1;
                    }
                }
            }

            // Determine which node to evaluate
            best_node = null;
            double best_node_score = 0;
            double playout_log = Math.Log(total_playouts, b); // Calculated in advance to reduce unnecessary calculations
            foreach (Node node in parent_nodes) {
                double this_node_score = ((node.wins + 0.1) / (node.playouts + 0.1)) + (c * Math.Sqrt(Math.Abs(playout_log / (node.playouts + 0.1))));

                if (best_node == null) {
                    best_node = node;
                    best_node_score = this_node_score;
                } else if (this_node_score > best_node_score) {
                    best_node = node;
                    best_node_score = this_node_score;
                }
            }

            // Apply the chosen node and prepare to evaluate it.
            new_board.ApplyMove(best_node.move);

            // Name the node move if it hasn't already been done
            if (best_node.move_name == null) {
                Move[] node_moves = new Move[parent_nodes.Count];
                for (int i = 0; i < parent_nodes.Count; i++) {
                    node_moves[i] = parent_nodes[i].move;
                }

                best_node.move_name = best_node.move.NameMove(board, new_board, node_moves, new_board.ListMoves());
            }

            if (best_node.children.Count == 0) { // Create new nodes
                Move[] next_moves = new_board.ListMoves(); // Possible reduncancy from earlier calculation
                ReturnData return_data = new ReturnData();

                foreach (Move move in next_moves) {
                    Node new_node = new Node(move);
                    best_node.children.Add(new_node);
                    new_node.turn = new_board.turn;

                    // Simulation
                    Board temp_board = new_board.Duplicate();
                    temp_board.ApplyMove(new_node.move);
                    int result = temp_board.Playout();

                    new_node.playouts += 1;
                    return_data.playouts += 1;
                    if (result == -1) {
                        new_node.wins += 0.5f;
                        return_data.white_wins += 0.5f;
                        return_data.black_wins += 0.5f;
                    } else if (result == 0) {
                        return_data.white_wins += 1;
                        if (new_node.turn == 0) {
                            new_node.wins += 1;
                        }
                    } else if (result == 1) {
                        return_data.black_wins += 1;
                        if (new_node.turn == 1) {
                            new_node.wins += 1;
                        }
                    }
                }

                return return_data;
                
            } else { // Check next nodes
                ReturnData result = MonteCarlo(new_board, best_node.children, b, c, random);

                best_node.playouts += result.playouts;
                if (best_node.turn == 0) {
                    best_node.wins += result.white_wins;
                } else {
                    best_node.wins += result.black_wins;
                }

                return result;
            }
        }

        public string FindBestLine(List<Node> nodes, int depth) {
            string line = "";

            Node top_node = null;
            foreach (Node node in nodes) {
                if (top_node == null) {
                    top_node = node;
                } else if (node.playouts > top_node.playouts) {
                    top_node = node;
                }
            }

            if (top_node.children.Count > 0 && depth > 1) {
                line += FindBestLine(top_node.children, depth - 1);
            }

            line = top_node.move_name + ' ' + line;
            return line;
        }

        public class Node {

            public List<Node> children = new List<Node>();
            public Move move;
            public string move_name;
            public int turn;
            public float wins = 0; // Can have 0.5 values for draws
            public int playouts = 0;

            public Node(Move input_move) {
                move = input_move;
            }
        }

        // Should be changed to test if this position existed earlier in time
        public bool FindTie(List<Move> moves) { // Determines if the final four moves are equal to the four moves beforehand
            if (moves.Count < 12) {
                return false;
            }
            if (moves.Count > 100) {
                return true;
            }

            if (moves[moves.Count - 1].Equals(moves[moves.Count - 4]) && moves[moves.Count - 2].Equals(moves[moves.Count - 5]) &&
                   moves[moves.Count - 3].Equals(moves[moves.Count - 6])) {
                return true;
            }

            if (moves[moves.Count - 1].Equals(moves[moves.Count - 5]) && moves[moves.Count - 2].Equals(moves[moves.Count - 6]) &&
                   moves[moves.Count - 3].Equals(moves[moves.Count - 7]) && moves[moves.Count - 4].Equals(moves[moves.Count - 8])) {
                return true;
            }

            if (moves[moves.Count - 1].Equals(moves[moves.Count - 7]) && moves[moves.Count - 2].Equals(moves[moves.Count - 8]) &&
                   moves[moves.Count - 3].Equals(moves[moves.Count - 9]) && moves[moves.Count - 4].Equals(moves[moves.Count - 10]) &&
                   moves[moves.Count - 5].Equals(moves[moves.Count - 11]) && moves[moves.Count - 6].Equals(moves[moves.Count - 12])) {
                return true;
            }

            return false;
        }

        public class Board { // Data class - stores piece position and castling status. No previous moves are recorded (Decoded FEN)
            // FEN datatype in order
            // Example FEN: rnbqkbnr/pp1ppppp/8/2p5/4P3/8/PPPP1PPP/RNBQKBNR w KQkq c6 0 2
            // Piece data (capitals are white, moving from the top left, moving right first, then descending)
            // Turn (w or b), castling availability, en passant pawn (only file need be stored), halfmoves since pawn advance or piece capture,
            // Total number of moves incremented after black's move.
            // Time stamp may need to be added on to the FEN.

            public string[,] piece_data = new string[8, 8];
            public int turn = 0; // 0 is white to move, 1 is black to move
            public bool white_castle_kingside = true; // Stores whether or not castling can occur in the future.
            public bool white_castle_queenside = true;
            public bool black_castle_kingside = true;
            public bool black_castle_queenside = true;
            public int passantFile = -1; // -1 is no file, 0 is a file, 7 is h file (pawn that can be taken via en passant)

            public Board(bool generateEmptyPieces = true) { // Generate an empty board.
                if (generateEmptyPieces) {
                    for (int y = 0; y < 8; y++) {
                        if (y == 0) {
                            piece_data[0, 0] = "wr";
                            piece_data[1, 0] = "wn";
                            piece_data[2, 0] = "wb";
                            piece_data[3, 0] = "wq";
                            piece_data[4, 0] = "wk";
                            piece_data[5, 0] = "wb";
                            piece_data[6, 0] = "wn";
                            piece_data[7, 0] = "wr";
                        } else if (y == 7) {
                            piece_data[0, 7] = "br";
                            piece_data[1, 7] = "bn";
                            piece_data[2, 7] = "bb";
                            piece_data[3, 7] = "bq";
                            piece_data[4, 7] = "bk";
                            piece_data[5, 7] = "bb";
                            piece_data[6, 7] = "bn";
                            piece_data[7, 7] = "br";
                        } else {
                            for (int x = 0; x < 8; x++) {
                                if (y == 1) {
                                    piece_data[x, 1] = "wp";
                                } else if (y == 6) {
                                    piece_data[x, 6] = "bp";
                                } else {
                                    piece_data[x, y] = "ee";
                                }
                            }
                        }
                    }
                }
            }

            public int Fitness() { // Evaluate position
                int fitness = 0;
                for (int y = 0; y < 8; y++) {
                    for (int x = 0; x < 8; x++) {

                        string piece = piece_data[x, y];
                        if (piece[1] == 'q') {
                            if (piece[0] == 'w') { fitness += 900; } else { fitness -= 900; }
                        }
                        if (piece[1] == 'r') {
                            if (piece[0] == 'w') { fitness += 500; } else { fitness -= 500; }
                        }
                        if (piece[1] == 'b') {
                            if (piece[0] == 'w') { fitness += 300; } else { fitness -= 300; }
                        }
                        if (piece[1] == 'n') {
                            if (piece[0] == 'w') { fitness += 300; } else { fitness -= 300; }
                        }
                        if (piece[1] == 'p') {
                            if (piece[0] == 'w') { fitness += 100; } else { fitness -= 100; }
                        }
                    }
                }

                return fitness;
            }

            public void ApplyMove(Move move) {
                if (turn == 0) {
                    turn = 1;
                } else {
                    turn = 0;
                }
                // Universal rules for moving pieces: the previous square will be empty, and the new square is overriden.
                piece_data[move.to_x, move.to_y] = piece_data[move.from_x, move.from_y];
                piece_data[move.from_x, move.from_y] = "ee";

                // En passant rules
                if (move.en_passant) {
                    if (piece_data[move.to_x, move.to_y][0] == 'w') { // White pawn en-passant
                        piece_data[move.to_x, move.to_y - 1] = "ee";
                    } else { // Black pawn en-passant
                        piece_data[move.to_x, move.to_y + 1] = "ee";
                    }
                    passantFile = move.to_x;
                } else {
                    passantFile = -1;
                }

                // Castling move
                if (move.castle) {
                    if (move.to_y == 0) {
                        white_castle_kingside = false;
                        white_castle_queenside = false;
                        if (move.to_x == 6) { // White kingside
                            piece_data[5, 0] = "wr";
                            piece_data[7, 0] = "ee";
                        } else { // White queenside
                            piece_data[3, 0] = "wr";
                            piece_data[0, 0] = "ee";
                        }
                    } else {
                        black_castle_kingside = false;
                        black_castle_queenside = false;
                        if (move.to_x == 6) { // Black kingside
                            piece_data[5, 7] = "br";
                            piece_data[7, 7] = "ee";
                        } else { // Black queenside
                            piece_data[3, 7] = "br";
                            piece_data[0, 7] = "ee";
                        }
                    }
                }

                // Cancel castling priviledge if either king or rook moves
                if (move.from_x == 0 && move.from_y == 0) {
                    white_castle_queenside = false;
                }
                if (move.from_x == 7 && move.from_y == 0) {
                    white_castle_kingside = false;
                }
                if (move.from_x == 0 && move.from_y == 7) {
                    black_castle_queenside = false;
                }
                if (move.from_x == 7 && move.from_y == 7) {
                    black_castle_kingside = false;
                }
                if (move.from_x == 4 && move.from_y == 0) {
                    white_castle_queenside = false;
                    white_castle_kingside = false;
                }
                if (move.from_x == 4 && move.from_y == 7) {
                    black_castle_queenside = false;
                    black_castle_kingside = false;
                }

                // Promotion rules
                if (move.promote) {
                    piece_data[move.to_x, move.to_y] = string.Concat(piece_data[move.to_x, move.to_y][0].ToString(),
                                                                               move.promotion_piece.ToString());
                }
            }

            public Board Duplicate() { // Return a deep copy of this board.
                return new Board(false) {
                    piece_data = piece_data.Clone() as string[,],
                    turn = turn,
                    white_castle_kingside = white_castle_kingside,
                    white_castle_queenside = white_castle_queenside,
                    black_castle_kingside = black_castle_kingside,
                    black_castle_queenside = black_castle_queenside,
                    passantFile = passantFile
                };
            }

            public Move[] ListMoves(int player = -1, bool check_search = true) { // Lists all moves in this position, including self-check ones (eliminated in minimax)

                // Obtain information from the board object (Rewrite later)
                Board board = this;
                if (player == -1) {
                    player = this.turn;
                }

                List<Move> moves = new List<Move>();
                string[,] piece_data = board.piece_data;

                // Iterate through each tile on the board and list all of the moves that can be played in a position.
                for (int y = 0; y < 8; y++) {
                    for (int x = 0; x < 8; x++) {
                        string piece = piece_data[x, y];
                        if (piece != "ee") {
                            if (piece[1] == 'p') {
                                if (piece[0] == 'w') { // White pawns
                                    if (y < 7) {
                                        if (piece_data[x, y + 1] == "ee") {
                                            if (y == 6) { // Promote
                                                moves.Add(new Move(x, y, x, y + 1) { promote = true, promotion_piece = 'q' });
                                                moves.Add(new Move(x, y, x, y + 1) { promote = true, promotion_piece = 'n' });
                                                moves.Add(new Move(x, y, x, y + 1) { promote = true, promotion_piece = 'r' });
                                                moves.Add(new Move(x, y, x, y + 1) { promote = true, promotion_piece = 'b' });
                                            } else { // Move 1 forward
                                                moves.Add(new Move(x, y, x, y + 1));
                                                if (y == 1 && piece_data[x, y + 2] == "ee") { // Move 2 forward
                                                    moves.Add(new Move(x, y, x, y + 2));
                                                }
                                            }
                                        }
                                    }
                                    if (x > 0) {
                                        if (piece_data[x - 1, y + 1][0] == 'b') {
                                            if (y == 6) { // Capture promotion
                                                moves.Add(new Move(x, y, x - 1, y + 1) { promote = true, promotion_piece = 'q' });
                                                moves.Add(new Move(x, y, x - 1, y + 1) { promote = true, promotion_piece = 'n' });
                                                moves.Add(new Move(x, y, x - 1, y + 1) { promote = true, promotion_piece = 'r' });
                                                moves.Add(new Move(x, y, x - 1, y + 1) { promote = true, promotion_piece = 'b' });
                                            } else { // Capture
                                                moves.Add(new Move(x, y, x - 1, y + 1));
                                            }
                                        } else if (x - 1 == board.passantFile) { // En passant
                                            moves.Add(new Move(x, y, x - 1, y + 1) { en_passant = true });
                                        }
                                    }
                                    if (x < 7) {
                                        if (piece_data[x + 1, y + 1][0] == 'b') {
                                            if (y == 6) { // Capture promotion
                                                moves.Add(new Move(x, y, x + 1, y + 1) { promote = true, promotion_piece = 'q' });
                                                moves.Add(new Move(x, y, x + 1, y + 1) { promote = true, promotion_piece = 'n' });
                                                moves.Add(new Move(x, y, x + 1, y + 1) { promote = true, promotion_piece = 'r' });
                                                moves.Add(new Move(x, y, x + 1, y + 1) { promote = true, promotion_piece = 'b' });
                                            } else { // Capture
                                                moves.Add(new Move(x, y, x + 1, y + 1));
                                            }
                                        } else if (x + 1 == board.passantFile) { // En passant
                                            moves.Add(new Move(x, y, x + 1, y + 1) { en_passant = true });
                                        }
                                    }
                                } else { // Black pawns
                                    if (y > 0) {
                                        if (piece_data[x, y - 1] == "ee") {
                                            if (y == 1) { // Promote
                                                moves.Add(new Move(x, y, x, y - 1) { promote = true, promotion_piece = 'q' });
                                                moves.Add(new Move(x, y, x, y - 1) { promote = true, promotion_piece = 'n' });
                                                moves.Add(new Move(x, y, x, y - 1) { promote = true, promotion_piece = 'r' });
                                                moves.Add(new Move(x, y, x, y - 1) { promote = true, promotion_piece = 'b' });
                                            } else { // Move 1 forward
                                                moves.Add(new Move(x, y, x, y - 1));
                                                if (y == 6 && piece_data[x, y - 2] == "ee") { // Move 2 forward
                                                    moves.Add(new Move(x, y, x, y - 2));
                                                }
                                            }
                                        }
                                    }
                                    if (x > 0) {
                                        if (piece_data[x - 1, y - 1][0] == 'w') {
                                            if (y == 1) { // Capture promotion
                                                moves.Add(new Move(x, y, x - 1, y - 1) { promote = true, promotion_piece = 'q' });
                                                moves.Add(new Move(x, y, x - 1, y - 1) { promote = true, promotion_piece = 'n' });
                                                moves.Add(new Move(x, y, x - 1, y - 1) { promote = true, promotion_piece = 'r' });
                                                moves.Add(new Move(x, y, x - 1, y - 1) { promote = true, promotion_piece = 'b' });
                                            } else { // Capture
                                                moves.Add(new Move(x, y, x - 1, y - 1));
                                            }
                                        } else if (x - 1 == board.passantFile) { // En passant
                                            moves.Add(new Move(x, y, x - 1, y - 1) { en_passant = true });
                                        }
                                    }
                                    if (x < 7) {
                                        if (piece_data[x + 1, y - 1][0] == 'w') {
                                            if (y == 1) { // Capture promotion
                                                moves.Add(new Move(x, y, x + 1, y - 1) { promote = true, promotion_piece = 'q' });
                                                moves.Add(new Move(x, y, x + 1, y - 1) { promote = true, promotion_piece = 'n' });
                                                moves.Add(new Move(x, y, x + 1, y - 1) { promote = true, promotion_piece = 'r' });
                                                moves.Add(new Move(x, y, x + 1, y - 1) { promote = true, promotion_piece = 'b' });
                                            } else { // Capture
                                                moves.Add(new Move(x, y, x + 1, y - 1));
                                            }
                                        } else if (x + 1 == board.passantFile) { // En passant
                                            moves.Add(new Move(x, y, x + 1, y - 1) { en_passant = true });
                                        }
                                    }
                                }
                            }
                            if (piece[1] == 'n') { // Knights
                                if (x > 0) {
                                    if (y > 1) {
                                        moves.Add(new Move(x, y, x - 1, y - 2));
                                    }
                                    if (y < 6) {
                                        moves.Add(new Move(x, y, x - 1, y + 2));
                                    }
                                    if (x > 1) {
                                        if (y > 0) {
                                            moves.Add(new Move(x, y, x - 2, y - 1));
                                        }
                                        if (y < 7) {
                                            moves.Add(new Move(x, y, x - 2, y + 1));
                                        }
                                    }
                                }
                                if (x < 7) {
                                    if (y > 1) {
                                        moves.Add(new Move(x, y, x + 1, y - 2));
                                    }
                                    if (y < 6) {
                                        moves.Add(new Move(x, y, x + 1, y + 2));
                                    }
                                    if (x < 6) {
                                        if (y > 0) {
                                            moves.Add(new Move(x, y, x + 2, y - 1));
                                        }
                                        if (y < 7) {
                                            moves.Add(new Move(x, y, x + 2, y + 1));
                                        }
                                    }
                                }
                            }
                            if (piece[1] == 'r' || piece[1] == 'q') { // Rooks and queens
                                if (x > 0) { // Move left
                                    for (int dx = x - 1; dx >= 0; dx--) {
                                        if (piece_data[dx, y][0] == piece[0]) { // Rook cannot move to or past its own color
                                            break;
                                        }
                                        moves.Add(new Move(x, y, dx, y)); // Otherwise the rook can move to this square.
                                        if (piece_data[dx, y] != "ee") { // If a square is not empty, it prevents the rook from going beyond it.
                                            break;
                                        }
                                    }
                                }
                                if (x < 7) { // Move right
                                    for (int dx = x + 1; dx <= 7; dx++) {
                                        if (piece_data[dx, y][0] == piece[0]) { // Rook cannot move to or past its own color
                                            break;
                                        }
                                        moves.Add(new Move(x, y, dx, y)); // Otherwise the rook can move to this square.
                                        if (piece_data[dx, y] != "ee") { // If a square is not empty, it prevents the rook from going beyond it.
                                            break;
                                        }
                                    }
                                }
                                if (y > 0) { // Move down
                                    for (int dy = y - 1; dy >= 0; dy--) {
                                        if (piece_data[x, dy][0] == piece[0]) { // Rook cannot move to or past its own color
                                            break;
                                        }
                                        moves.Add(new Move(x, y, x, dy)); // Otherwise the rook can move to this square.
                                        if (piece_data[x, dy] != "ee") { // If a square is not empty, it prevents the rook from going beyond it.
                                            break;
                                        }
                                    }
                                }
                                if (y < 7) { // Move up
                                    for (int dy = y + 1; dy <= 7; dy++) {
                                        if (piece_data[x, dy][0] == piece[0]) { // Rook cannot move to or past its own color
                                            break;
                                        }
                                        moves.Add(new Move(x, y, x, dy)); // Otherwise the rook can move to this square.
                                        if (piece_data[x, dy] != "ee") { // If a square is not empty, it prevents the rook from going beyond it.
                                            break;
                                        }
                                    }
                                }
                            }
                            if (piece[1] == 'b' || piece[1] == 'q') { // Bishops and queens
                                if (x > 0 && y > 0) { // Down left
                                    for (int d = 1; d <= 7; d++) {
                                        if (x - d < 0 || y - d < 0) { // Out of bounds
                                            break;
                                        }
                                        if (piece_data[x - d, y - d][0] == piece[0]) { // Bishop cannot move to or past its own color
                                            break;
                                        }
                                        moves.Add(new Move(x, y, x - d, y - d)); // Otherwise the bishop can move to this square.
                                        if (piece_data[x - d, y - d] != "ee") { // If a square is not empty, it prevents the bishop from going beyond it.
                                            break;
                                        }
                                    }
                                }
                                if (x > 0 && y < 7) { // Up left
                                    for (int d = 1; d <= 7; d++) {
                                        if (x - d < 0 || y + d > 7) { // Out of bounds
                                            break;
                                        }
                                        if (piece_data[x - d, y + d][0] == piece[0]) { // Bishop cannot move to or past its own color
                                            break;
                                        }
                                        moves.Add(new Move(x, y, x - d, y + d)); // Otherwise the bishop can move to this square.
                                        if (piece_data[x - d, y + d] != "ee") { // If a square is not empty, it prevents the bishop from going beyond it.
                                            break;
                                        }
                                    }
                                }
                                if (x < 7 && y < 7) { // Up right
                                    for (int d = 1; d <= 7; d++) {
                                        if (x + d > 7 || y + d > 7) { // Out of bounds
                                            break;
                                        }
                                        if (piece_data[x + d, y + d][0] == piece[0]) { // Bishop cannot move to or past its own color
                                            break;
                                        }
                                        moves.Add(new Move(x, y, x + d, y + d)); // Otherwise the bishop can move to this square.
                                        if (piece_data[x + d, y + d] != "ee") { // If a square is not empty, it prevents the bishop from going beyond it.
                                            break;
                                        }
                                    }
                                }
                                if (x < 7 && y > 0) { // Down right
                                    for (int d = 1; d <= 7; d++) {
                                        if (x + d > 7 || y - d < 0) { // Out of bounds
                                            break;
                                        }
                                        if (piece_data[x + d, y - d][0] == piece[0]) { // Bishop cannot move to or past its own color
                                            break;
                                        }
                                        moves.Add(new Move(x, y, x + d, y - d)); // Otherwise the bishop can move to this square.
                                        if (piece_data[x + d, y - d] != "ee") { // If a square is not empty, it prevents the bishop from going beyond it.
                                            break;
                                        }
                                    }
                                }
                            }
                            if (piece[1] == 'k') {
                                if (x > 0) {
                                    moves.Add(new Move(x, y, x - 1, y));
                                    if (y > 0) {
                                        moves.Add(new Move(x, y, x - 1, y - 1));
                                    }
                                    if (y < 7) {
                                        moves.Add(new Move(x, y, x - 1, y + 1));
                                    }
                                }
                                if (y > 0) {
                                    moves.Add(new Move(x, y, x, y - 1));
                                }
                                if (y < 7) {
                                    moves.Add(new Move(x, y, x, y + 1));
                                }
                                if (x < 7) {
                                    moves.Add(new Move(x, y, x + 1, y));
                                    if (y > 0) {
                                        moves.Add(new Move(x, y, x + 1, y - 1));
                                    }
                                    if (y < 7) {
                                        moves.Add(new Move(x, y, x + 1, y + 1));
                                    }
                                }
                            }
                        }
                        /*if (!check_search) {
                            foreach (Move move in moves) { // See if a king can be captured with moves found thus far.
                                if ((player == 0 && board.piece_data[move.from_x, move.from_y][0] == 'w' &&
                                    board.piece_data[move.to_x, move.to_y] == "bk") ||
                                    (player == 1 && board.piece_data[move.from_x, move.from_y][0] == 'b' &&
                                    board.piece_data[move.to_x, move.to_y] == "wk")) {

                                    return new Move[1] { move };
                                }
                            }

                            moves = new List<Move>();
                        }*/
                    }
                }
                /*if (!check_search) {
                    return new Move[0];
                }*/

                // Filter self-capturing moves and add castles.
                List<Move> approvedMoves = new List<Move>(); // Moves that don't capture one's own pieces, and are either all black or white moves.
                bool wCastleKing = board.white_castle_kingside; // Also determine if a castle can be played THIS MOVE.
                bool wCastleQueen = board.white_castle_queenside;
                bool bCastleKing = board.black_castle_kingside;
                bool bCastleQueen = board.black_castle_queenside;
                foreach (Move move in moves) {
                    if (piece_data[move.from_x, move.from_y][0] != piece_data[move.to_x, move.to_y][0]) {
                        if (player == 0 && piece_data[move.from_x, move.from_y][0] == 'w') { // Ensure moves do not allow king captures
                            if (check_search) {
                                Board temp_board = board.Duplicate();
                                temp_board.ApplyMove(move);
                                Move[] responses = temp_board.ListMoves(1, false);
                                bool approve_move = true;

                                foreach (Move response in responses) {
                                    if (temp_board.piece_data[response.to_x, response.to_y] == "wk" && temp_board.piece_data[response.from_x, response.from_y][0] == 'b') {
                                        approve_move = false;
                                        break;
                                    }
                                }
                                if (approve_move) {
                                    approvedMoves.Add(move);
                                }
                            } else {
                                approvedMoves.Add(move);
                            }
                        } else if (player == 1 && piece_data[move.from_x, move.from_y][0] == 'b') {
                            if (check_search) {
                                Board temp_board = board.Duplicate();
                                temp_board.ApplyMove(move);
                                Move[] responses = temp_board.ListMoves(0, false);
                                bool approve_move = true;

                                foreach (Move response in responses) {
                                    if (temp_board.piece_data[response.to_x, response.to_y] == "bk" && temp_board.piece_data[response.from_x, response.from_y][0] == 'w') {
                                        approve_move = false;
                                        break;
                                    }
                                }
                                if (approve_move) {
                                    approvedMoves.Add(move);
                                }
                            } else {
                                approvedMoves.Add(move);
                            }
                        }
                    }
                    // Prevent castling if certain squares are controlled.
                    if ((move.to_x == 4 || move.to_x == 5) && move.to_y == 0 && piece_data[move.from_x, move.from_y][0] == 'b') {
                        wCastleKing = false;
                    }
                    if ((move.to_x == 4 || move.to_x == 3) && move.to_y == 0 && piece_data[move.from_x, move.from_y][0] == 'b') {
                        wCastleQueen = false;
                    }
                    if ((move.to_x == 4 || move.to_x == 5) && move.to_y == 7 && piece_data[move.from_x, move.from_y][0] == 'w') {
                        bCastleKing = false;
                    }
                    if ((move.to_x == 4 || move.to_x == 3) && move.to_y == 7 && piece_data[move.from_x, move.from_y][0] == 'w') {
                        bCastleQueen = false;
                    }
                }
                if (piece_data[5, 0] != "ee" || piece_data[6, 0] != "ee") {
                    wCastleKing = false;
                }
                if (piece_data[1, 0] != "ee" || piece_data[2, 0] != "ee" || piece_data[3, 0] != "ee") {
                    wCastleQueen = false;
                }
                if (piece_data[5, 7] != "ee" || piece_data[6, 7] != "ee") {
                    bCastleKing = false;
                }
                if (piece_data[1, 7] != "ee" || piece_data[2, 7] != "ee" || piece_data[3, 7] != "ee") {
                    bCastleQueen = false;
                }
                if (wCastleKing && player == 0) { approvedMoves.Add(new Move(4, 0, 6, 0) { castle = true, plausibility = 100 }); }
                if (wCastleQueen && player == 0) { approvedMoves.Add(new Move(4, 0, 2, 0) { castle = true, plausibility = 100 }); }
                if (bCastleKing && player == 1) { approvedMoves.Add(new Move(4, 7, 6, 7) { castle = true, plausibility = 100 }); }
                if (bCastleQueen && player == 1) { approvedMoves.Add(new Move(4, 7, 2, 7) { castle = true, plausibility = 100 }); }

                return approvedMoves.ToArray();
            }

            public int CountKings() { // -1 if both sides still have pieces, 0 if only white king, 1 if only black king
                int w_counter = 0;
                int b_counter = 0;
                for (int x = 0; x < 8; x++) {
                    for (int y = 0; y < 8; y++) {
                        if (piece_data[x, y][0] == 'w') {
                            w_counter += 1;
                        } else if (piece_data[x, y][0] == 'b') {
                            b_counter += 1;
                        }
                    }
                }
                if (w_counter == 1) {
                    return 0;
                }
                if (b_counter == 1) {
                    return 1;
                }
                return -1;
            }

            public int Playout() { // -1 is draw, 0 is white win, 1 is black win
                Board board = Duplicate();
                Random random = new Random();
                Move[] moves;
                Move chosen_move;

                bool protect_king = false; // Should the safety of the king be considered? (King capture would be a win)
                bool use_minimax = true;

                int counter = 0;

                bool skip_white = false;
                if (board.turn == 1) {
                    skip_white = true;
                }

                int best_fitness;

                while (true) {
                    counter += 1;
                    if (!skip_white) {
                        moves = board.ListMoves(0, protect_king);
                        if (moves.Length == 0) {
                            // Detect checkmate/stalemate here.
                            bool check = false;
                            foreach (Move move in moves) {
                                if (board.piece_data[move.to_x, move.to_y] == "wk" && board.piece_data[move.from_x, move.from_y][0] == 'b') {
                                    check = true;
                                    break;
                                }
                            }
                            if (check) {
                                return 1;
                            } else {
                                return -1;
                            }
                        } else if (board.CountKings() == 1) {
                            return 0;
                        }

                        //chosen_move = moves[random.Next(moves.Length)]; // Pick a random move
                        
                        chosen_move = null; // Pick the best looking move
                        best_fitness = -100;
                        foreach (Move move in moves) {
                            if (chosen_move == null) {
                                chosen_move = move;
                            } else {
                                Board board_copy = board.Duplicate();
                                board_copy.ApplyMove(move);
                                int fitness = board_copy.Fitness();

                                if (fitness > best_fitness) {
                                    chosen_move = move;
                                    best_fitness = fitness;
                                }
                            }
                        }

                        if (board.piece_data[chosen_move.to_x, chosen_move.to_y][1] == 'k') {
                            return 0; // White captured black's king and wins.
                        }

                        board.ApplyMove(chosen_move);
                    } else {
                        skip_white = false;
                    }

                    moves = board.ListMoves(1, protect_king);
                    if (moves.Length == 0) {
                        // Detect checkmate/stalemate here.
                        bool check = false;
                        foreach (Move move in moves) {
                            if (board.piece_data[move.to_x, move.to_y] == "bk" && board.piece_data[move.from_x, move.from_y][0] == 'w') {
                                check = true;
                                break;
                            }
                        }
                        if (check) {
                            return 0;
                        } else {
                            return -1;
                        }
                    } else if (board.CountKings() == 0) {
                        return 1;
                    }

                    //chosen_move = moves[random.Next(moves.Length)]; // Pick a random move

                    chosen_move = null; // Pick the best looking move
                    best_fitness = 100;
                    foreach (Move move in moves) {
                        if (chosen_move == null) {
                            chosen_move = move;
                        } else {
                            Board board_copy = board.Duplicate();
                            board_copy.ApplyMove(move);
                            int fitness = board_copy.Fitness();

                            if (fitness < best_fitness) {
                                chosen_move = move;
                                best_fitness = fitness;
                            }
                        }
                    }

                    if (board.piece_data[chosen_move.to_x, chosen_move.to_y][1] == 'k') {
                        return 1; // Black captured white's king and wins.
                    }

                    board.ApplyMove(chosen_move);

                    if (counter > 40) { // Max playout length
                        return -1;
                    }
                }
            }

            public void Print() {
                Console.WriteLine();
                for (int y = 0; y < 8; y++) {
                    string newline = "";
                    for (int x = 0; x < 8; x++) {
                        string piece = piece_data[x, 7 - y];
                        if (piece == "ee") {
                            newline += "  ";
                        } else {
                            newline += piece;
                        }
                        if (x < 7) {
                            newline += " | ";
                        }
                    }
                    Console.WriteLine(newline);
                    if (y < 7) {
                        Console.WriteLine("-------------------------------------");
                    }
                }
                Console.WriteLine();
            }
        }

        public class Move { // Move class - stores coordinates for piece origin and destination

            public int from_x;
            public int from_y;
            public int to_x;
            public int to_y;

            public bool en_passant = false;
            public bool castle = false;
            public bool promote = false;
            public char promotion_piece;

            public int fitness;
            public int plausibility = 0; // Used in sorting heuristic
            public Move Response; // Used by minimax algorithm to store lines.

            public Move(int x1, int y1, int x2, int y2) {
                from_x = x1;
                from_y = y1;
                to_x = x2;
                to_y = y2;
            }

            public bool Equals(Move move) { // Returns whether a given move is the same as this one
                return move.to_x == to_x && move.to_y == to_y && move.from_x == from_x && move.from_y == from_y;
            }

            public string NameMove(Board board, Board new_board, Move[] all_moves, Move[] new_moves) { // Board refers to previous board state.
                if (castle) {
                    if (to_x == 6) {
                        return "O-O";
                    }
                    return "O-O-O";
                }

                string name;
                string files = "abcdefgh";
                char piece = Char.ToUpper(board.piece_data[from_x, from_y][1]);
                if (piece == 'P') { // Pawn naming
                    name = files[from_x].ToString();
                    if (board.piece_data[to_x, to_y] != "ee" || en_passant) { // Captures and en passant
                        name += "x";
                        name += files[to_x].ToString();
                    }
                    name += (to_y + 1).ToString();

                    if (promote) {
                        name += "=" + Char.ToUpper(promotion_piece);
                    }

                } else { // Any other piece naming
                    name = piece.ToString();

                    // Identify file / row if necessary
                    int same_file_count = 0;
                    int same_row_count = 0;
                    foreach (Move move in all_moves) {
                        if (move.to_x == to_x && move.to_y == to_y && board.piece_data[move.from_x, move.from_y] == board.piece_data[from_x, from_y]) {
                            if (from_x == move.from_x) {
                                same_file_count += 1;
                            }
                            if (from_y == move.from_y) {
                                same_row_count += 1;
                            }
                        }
                    }
                    if (same_row_count > 1) { // If two of the same pieces are in the same row, file must be indicated.
                        name += files[from_x];
                    }
                    if (same_file_count > 1) { // If two of the same pieces are in the same file, row must be indicated.
                        name += (from_x + 1).ToString();
                    }

                    if (board.piece_data[to_x, to_y] != "ee") { // Captures
                        name += "x";
                    }

                    name += files[to_x]; // Destination file
                    name += (to_y + 1).ToString(); // Destination row
                }

                /*
                if (new_moves.Length == 0) {
                    name += '#'; // Checkmate
                } else {
                    foreach (Move move in new_moves) {
                        if (new_board.piece_data[move.to_x, move.to_y][0] != new_board.piece_data[from_x, from_y][0] &&
                            new_board.piece_data[move.to_x, move.to_y][1] == 'k') {
                            name += '+'; // Check
                        }
                    }
                }*/

                return name;
            }

            public string GetCoordinateString() {
                return from_x.ToString() + from_y.ToString() + to_x.ToString() + to_y.ToString();
            }
        }

        public Move Minimax(Board board, int depth, float alpha, float beta, int player) {
            Move[] next_moves = board.ListMoves();
            if (next_moves == null) { // A king is captured, this branch is invalid
                //Console.WriteLine("Branch killed");
                return null;
            }

            if (next_moves.Length == 0) {
                //Console.WriteLine("There were no moves available (Please report this to developer)");
                return null; // Theoretical stalemate
            }
            if (depth == 0) {
                return new Move(0, 0, 0, 0) { fitness = board.Fitness() };
            }

            if (player == 0) { // Maximize white player's fitness
                Move best_move = new Move(0, 0, 0, 0) { fitness = -1000000 };
                foreach (Move move in next_moves) {
                    Board new_board = board.Duplicate();
                    new_board.ApplyMove(move);
                    Move next_move = Minimax(new_board, depth - 1, alpha, beta, 1);
                    if (next_move != null) { // Only consider valid moves
                        if (next_move.fitness >= best_move.fitness) {
                            best_move = move;
                            best_move.Response = next_move;
                            best_move.fitness = next_move.fitness;
                        }
                        if (best_move.fitness > alpha) {
                            alpha = best_move.fitness;
                        }
                        if (alpha > beta) {
                            break;
                        }
                    }
                }
                return best_move;

            } else { // Minimize black player's fitness
                Move best_move = new Move(0, 0, 0, 0) { fitness = 1000000 };
                foreach (Move move in next_moves) {
                    Board new_board = board.Duplicate();
                    new_board.ApplyMove(move);
                    Move next_move = Minimax(new_board, depth - 1, alpha, beta, 0);
                    if (next_move != null) { // Only consider valid moves
                        if (next_move.fitness <= best_move.fitness) {
                            best_move = move;
                            best_move.Response = next_move;
                            best_move.fitness = next_move.fitness;
                        }
                        if (best_move.fitness < beta) {
                            beta = best_move.fitness;
                        }
                        if (alpha > beta) {
                            break;
                        }
                    }
                }
                return best_move;

            }
        }

        private readonly Dictionary<char, int> fileNumbers = new Dictionary<char, int>() {
            { 'a', 0 }, {'b', 1 }, {'c', 2 }, {'d', 3 }, {'e', 4 }, {'f', 5 }, {'g', 6 }, {'h', 7}
        };

        private int Opp_Player(int player) {
            if (player == 0) { return 1; }
            return 0;
        }
    }
}