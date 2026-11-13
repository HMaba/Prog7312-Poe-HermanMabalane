# Prog7312-Poe-HermanMabalane

# Smart-X: Network Topology and Mesh Routing

PROG7312 Portfolio of Evidence, Part 3 (Final) Student: Herman Mabalane | Student number: ST10278399

Smart-X is an IoT monitoring system for a farm. Sensors report soil moisture, temperature, humidity, water flow and battery level. Operators watch live data, switch pumps and relays, and see how the sensors reach the central gateway through a mesh network. Part 3 adds that mesh view, built on a graph, a breadth-first search, a minimum spanning tree and a self-balancing AVL tree for historical queries.

# How to compile and run
Visual Studio 2026 (recommended)
Open Smart-X-POE.slnx.
Build, Rebuild Solution. It should finish with no errors.
Right-click the solution, choose Configure Startup Projects, select Multiple startup projects, and set both SmartAPI and SmartXWpf to Start (the Smart-X-POE.slnLaunch.user profile does this).
The API starts first on http://localhost:5109, then the WPF window opens.

# How to use the application
Sign in
# Login: doej@email.con Password: Password21
The sign-in window asks for your Firebase email and password. After a successful sign-in the main window opens. A guest option lets you view data without signing in. Actions that change data (sending commands, undo, failing or restoring a node, registering a device, uploading a file) require a signed-in user.

# What the application does

The main window has three modules. All three are active and share one running API.

Module	What it gives you
Sensor Data Ingestion and Telemetry	Dashboard with live telemetry and alerts, sensor list, telemetry view, alert list, facility power, and file upload
Real-Time Command Stream and History	Pump and relay commands with undo, device search and filter, live timeline, command and search history, and a Suggested Actions / Automated Insights panel
Network Topology and Mesh Routing	A drawn mesh of all sensors, the minimum spanning tree, route tracking by sensor, a fail/restore button for demonstrating self-healing, and a historical log with a time-range filter

Switching modules keeps the page you were on, because each page is created once and reused.

# Technology
Layer	Technology
Backend	ASP.NET Core Web API on .NET 10 (SmartAPI)
Frontend	WPF desktop app on .NET 10 (SmartXWpf), MVVM view models and page navigation
Authentication	Firebase Authentication (email and password). The WPF app signs in with Firebase and sends the ID token as a bearer token. The API validates it with JwtBearer
API documentation	Swagger UI (Swashbuckle)
Storage	In-memory (data resets when the API restarts)

# Firebase

The API validates tokens from the Firebase project set in SmartAPI/appsettings.json:
In the Firebase console, enable Authentication, Sign-in method, Email/Password.
Add at least one user (email and password).
Set Firebase:ProjectId to your project ID. It can also be set with user secrets: dotnet user-secrets set "Firebase:ProjectId"
In SmartXWpf/Services/AuthenticationService.cs, set the Firebase Web API key for the same project.


# Data structures and algorithms
# Summary #
Requirement,	Structure or algorithm,	Where,	and Why it was chosen

Historical queries by time range	AVL tree (self-balancing BST)	AvlTree.cs, DataHub.History	Telemetry arrives in timestamp order. A plain tree would become a linked list. AVL stays balanced, so inserts and range queries stay fast

Model the mesh	Weighted undirected graph (adjacency list)	MeshGraph.cs	A mesh is a set of devices with links between them. Edges carry a power cost

Which devices are connected or down	Breadth-first search (BFS)	MeshGraph.Bfs	Visits every node reachable from the gateway. Anything not visited is unreachable

Most power-efficient routing	Prim's minimum spanning tree	MeshGraph.Prim	Connects all reachable nodes to the gateway at the lowest total link cost

Route of one sensor	Parent walk up the MST	MeshGraph.RouteToGateway	Follows each node's MST parent until the gateway

Telemetry processing	Queue<T>	DataHub.TelemetryQueue	First in, first out for incoming packets

Critical alerts first	PriorityQueue<T, int>	DataHub.CriticalAlertQueue	Critical alerts have priority 0 and are processed before telemetry

Command undo	Stack<T>	DataHub.CommandUndoStack	Last command is the first undone

Device lookup	Dictionary<string, Device>	DataHub.DeviceRegistry	Instant lookup by MAC address, no list scanning

Ordered live timeline	SortedDictionary<DateTime, T>	DataHub.SensorTimeline	Always in time order for the timeline

No duplicate alerts	HashSet<string>	DataHub.ActiveErrors	An alert condition can only be active once

# AVL tree: historical telemetry

Every processed reading is inserted into the tree for its sensor, keyed by timestamp.

After each insert the path back to the root is rebalanced. If a node's left and right heights differ by more than one, it is fixed with a single or double rotation.

The range query only walks branches that can contain matching timestamps. Whole subtrees outside the range are skipped.

Results come back in timestamp order because of the in-order walk. A reversed range (from later than to) returns an empty list. The API turns that into a 400 Bad Request.

Why a tree? Pulling "all readings between 14:00 and 16:00 for Node A" from an unordered list means checking every reading. The tree reaches the start of the range in about log n steps and then only visits the matching readings.
