open System
open System.Net
open System.Net.Sockets
open System.Text
open System.IO
open System.Threading

let IPADDRESS = "127.0.0.1"
let PORT = 5666
let ENFORMAT = "utf-8"
let mutable flag = false 
type ClientInfo = {
    ClientId: int
    Socket: Socket
}
let terminationEvent = new ManualResetEvent(false)

let connectedClients = System.Collections.Concurrent.ConcurrentBag<ClientInfo>()


let parseCommand (command: string) =
    try
        
        let parts = command.Split(' ')
        if parts.Length >= 3 && parts.Length <= 5 then
            let op = parts.[0]
            let operands = Array.zeroCreate<int> (parts.Length - 1)
            let mutable valid = true
            for i = 1 to parts.Length - 1 do
                let validOperand, operand = Int32.TryParse(parts.[i])
                if validOperand then
                    operands.[i - 1] <- operand
                else
                    valid <- false
                    flag <- true
            if valid then
                Some (op, operands)
            else
                None
        else
            None
    with
    | _ -> None

let handleClient (clientInfo: ClientInfo) =
    async {
        try
            

            let stream = new NetworkStream(clientInfo.Socket)
            connectedClients.Add(clientInfo)
            let reader = new StreamReader(stream, Encoding.GetEncoding(ENFORMAT))
            let writer = new StreamWriter(stream, Encoding.GetEncoding(ENFORMAT))
            
            printfn "Client%d connected from %s" clientInfo.ClientId (clientInfo.Socket.RemoteEndPoint.ToString())
            
            writer.WriteLine("Hello!")
            writer.Flush()
            
            while true do
                let request = reader.ReadLine()
                printfn "Recived: %A" request
                if request = "bye" then
                    writer.WriteLine("-5")
                    printfn "Responding to client %d with result: -5" clientInfo.ClientId 
                    writer.Flush()
                if request = "terminate" then
                    for stream in connectedClients do
                        try
                            let tempstream = new NetworkStream(stream.Socket)
                            let tempreader = new StreamReader(tempstream, Encoding.GetEncoding(ENFORMAT))
                            let tempwriter = new StreamWriter(tempstream, Encoding.GetEncoding(ENFORMAT))
                            //tempwriter.Flush()
                            tempwriter.WriteLine("-5")
                            tempwriter.Flush()
                            //stream.Socket.Close()
                        with
                        | _ -> ()
                    Environment.Exit(0)
                let parsedCommand = parseCommand request
                let parts = request.Split(' ')
                //printfn "checking %A"flag
                
                if flag then
                        flag <- false
                        writer.WriteLine("-4")
                        printfn "Responding to client %d with result: -4" clientInfo.ClientId 
                        //printfn "Error: One or more of the inputs contain non-numbers"
                        writer.Flush()
                else        

                    match parsedCommand with
                    | Some (op, operands) when op.ToLowerInvariant() = "add" && operands.Length < 2 ->
                        writer.WriteLine("-2")
                        printfn "Error: Number of inputs is less than two"
                        writer.Flush()
                    
                    | Some (op, operands) ->
                        let response = 
                            match op.ToLowerInvariant() with
                            | "add" -> sprintf "%d" (Array.sum operands)
                            | "subtract" -> sprintf "%d" (Array.reduce (-) operands)
                            | "multiply" -> sprintf "%d" (Array.fold (*) 1 operands)
                            | _ -> "-1"  
                        //printfn "Responding to client %d with result: %A" clientInfo.ClientId response
                        let success, value = Int32.TryParse(response)
                        
                        if success && value < 0 then
                            printfn "Responding to client %d with result: -1" clientInfo.ClientId 
                            writer.WriteLine("-1")
                            writer.Flush()
                        else
                            printfn "Responding to client %d with result: %A" clientInfo.ClientId value
                            writer.WriteLine(response)

                            writer.Flush()
                    
                    | None when parts.Length >= 6 ->
                        writer.WriteLine("-3")
                        writer.Flush()

                    | None when parts.Length <= 2 ->
                        writer.WriteLine("-2")
                        writer.Flush()
                        
                    | None ->
                        writer.WriteLine("-1")
                        writer.Flush()
                
        with
        | _ -> printf ""
    }
let main argv  =
    let listener = new TcpListener(IPAddress.Parse(IPADDRESS), PORT)
    listener.Start()
    printfn "Server is running and listenning on port %d." PORT
    
    let mutable clientId = 1
    while true do
        let clientSocket = listener.AcceptSocket()
        let currentClientId = Interlocked.Increment(&clientId) - 1
        let clientInfo = { ClientId = currentClientId; Socket = clientSocket }
        Async.Start (handleClient clientInfo)


#if INTERACTIVE
main [||]
#endif
