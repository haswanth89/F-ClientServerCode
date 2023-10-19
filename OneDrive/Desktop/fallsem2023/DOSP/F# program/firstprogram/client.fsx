open System
open System.Net
open System.Net.Sockets
open System.Text
open System.IO
open System.Threading

let IPADDRESS = "127.0.0.1"
let PORT = 5666
let ENFORMAT = "utf-8"
let mutable continueRunning = true

let listenForServerResponses (reader: StreamReader) (client: TcpClient)= 
    async {
        while continueRunning do
            try
                let mutable response = reader.ReadLine()
                let filteredString = new string(response |> Seq.filter (fun c -> int c >= 0 && int c <= 127) |> Seq.toArray)
                printfn "%s" filteredString
                match filteredString with
                | "-1" -> printfn "\nError: Invalid operation"
                | "-2" -> printfn "\nError: Number of inputs is less than two"
                | "-3" -> printfn "\nError: Number of inputs is more than four"
                | "-4" -> printfn "\nError: One or more of the inputs contain non-numbers"
                | "-5" -> 
                    printfn "\nServer is shutting down. Disconnecting client..."
                    client.Close()
                    Environment.Exit(1)
                    continueRunning <- false
                | _ -> printfn "\nServer response: %s" filteredString
            with
            | _ -> client.Close()
    }

let main args  =
    let client = new TcpClient(IPADDRESS, PORT)
    let stream = client.GetStream()
    let reader = new StreamReader(stream, Encoding.GetEncoding(ENFORMAT))
    let writer = new StreamWriter(stream, Encoding.GetEncoding(ENFORMAT))
    
    // Receive and print the "Hello!" message
    let helloMsg = reader.ReadLine()
    printfn "%s" helloMsg

    // Start the task to listen for server responses
    let listeningTask = Async.Start (listenForServerResponses reader client)

    // Main client loop
    while continueRunning do
        printf "sending command: "
        let input = Console.ReadLine()
        
        // Send the command to the server
        writer.WriteLine(input)
        writer.Flush()
        
        if input = "terminate" then
            client.Close()
            continueRunning <- false

        Thread.Sleep(100)  // Small sleep to allow async task to process responses
        
    client.Close()

[<EntryPoint>]
#if INTERACTIVE
// Automatically invoke the main function in FSI
main [||]
#endif
