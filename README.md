- This is a Microsoft specific project
- There are two projects within the solution
-	AOOClient is a PC aplication that will be the chat interfacer. It is written with Microsoft WPF technology
-	AOIServer is a web server based application - it is written with Microsoft [WEB] API technology
- The core communication infrastructure uses Microsoft SignalR
-	This technology utlizies different communication protocils but defaults to web sockts
-	the commuinication is a simple process of registration and connection from and to a server exposing a singlarR interface
- There is a copy of the client that will be packaged with the code. This version is configured to access a deployment of the AOIServer
-	it will be in a folder named ClientRuntime

