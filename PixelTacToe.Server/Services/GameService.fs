namespace PixelTacToe.Server.Services

open PixelTacToe.Shared.Constants

module GameService =
    
    let checkForWinner (board: string[,]) (marker: string) (row: int) (col: int) =
        let rowWin = 
            seq { 0 .. GameConstants.BoardSize - 1 }
            |> Seq.forall (fun c -> board.[row, c] = marker)
            
        let colWin = 
            seq { 0 .. GameConstants.BoardSize - 1 }
            |> Seq.forall (fun r -> board.[r, col] = marker)
            
        let diag1Win = 
            if row = col then
                seq { 0 .. GameConstants.BoardSize - 1 }
                |> Seq.forall (fun i -> board.[i, i] = marker)
            else false
            
        let diag2Win = 
            if row + col = GameConstants.BoardSize - 1 then
                seq { 0 .. GameConstants.BoardSize - 1 }
                |> Seq.forall (fun i -> board.[i, GameConstants.BoardSize - 1 - i] = marker)
            else false
            
        rowWin || colWin || diag1Win || diag2Win
        
    let isBoardFull (board: string[,]) =
        let mutable full = true
        for row in 0 .. GameConstants.BoardSize - 1 do
            for col in 0 .. GameConstants.BoardSize - 1 do
                if board.[row, col] = "" then
                    full <- false
        full 