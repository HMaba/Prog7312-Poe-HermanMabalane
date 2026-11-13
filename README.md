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
# Graph, BFS and minimum spanning tree

The graph stores each node's neighbours in a dictionary of lists, and every edge is stored in both directions.

BFS starts at the gateway and visits neighbours level by level using a queue. Nodes it never reaches are the unreachable (down or cut-off) devices.

Each MST edge records a child and its parent. A sensor's route is found by following parents from the sensor up to the gateway. If a node has no parent, it has no route.

Why the MST? It shows the lowest-cost set of links that still connects every reachable sensor to the gateway. Routing telemetry along it uses the least total transmission cost across the mesh.

BFS versus the MST: BFS finds the connected set (and the fewest hops). The MST finds the cheapest set of links. The route shown on the Topology page follows the MST.

# How the mesh network is built

The sensors are simulated, so the wireless links are generated by a fixed rule in DataHub.BuildGraph. The rule is deterministic, so the same devices always give the same mesh.

Take all non-actuator devices ordered by ID. Pumps and relays are not part of the mesh.
Remove any sensor that is currently marked disconnected.
Link each sensor to the next two sensors in the list. The link cost is (indexA + indexB) % 5 + 1.
Link the first three sensors in the list to the gateway. The cost is index % 5 + 1.

The graph is rebuilt on every topology request from the device registry and the set of disconnected nodes. That is what makes the network self-healing: when a node is removed, the remaining sensors are re-linked to their new neighbours, and the gateway links move to the first three sensors that are still alive.

# Predictive Action Engine

The engine records what the operator does and finds patterns from that history. It does not use random suggestions.

Record. Every command and every search is stored as a HistoryEvent together with the alert conditions that were active at that moment (for example Soil moisture below 30% (SENSOR-008)).
Count pairs. Each (active condition, action) pair has a counter. When the same action is taken under the same condition again, the counter goes up.
Suggest. When a pair has been seen 3 or more times and that condition is active right now, it appears under Suggested Actions, with the evidence, for example: "Pump A On: you did this 4 times when Soil moisture below 30% (SENSOR-008)."
Highlight before the user searches. Automated Insights lists, without being asked:
sensors that are offline,
sensors with a low battery (below 20%),
readings outside the normal range: moisture below 30%, temperature above 35 C, humidity outside 35 to 90%, water flow above 25 L/min.

The history is seeded with four patterns so the panel has content on first launch, and it grows as the operator uses the system.

# Tests
AvlTreeTests (7 tests) check the tree that stores telemetry history by timestamp:

Inserting items increases the count.
Querying an empty tree returns nothing.
A time-range query returns only readings inside the range, in chronological order, even if they were inserted out of order.
The start and end of the range are included.
A range where the start is after the end returns nothing.
Readings with the same timestamp are all kept.
Thousands of sequential inserts still give correct results.

MeshGraphTests (8 tests) check the sensor network graph:

Edges work in both directions, and self-loops and duplicate edges are ignored.
Breadth-first search visits nodes level by level, and an unknown start node returns an empty list.
Prim’s algorithm builds the cheapest spanning tree and skips the heavier edge, and an unknown start node returns an empty list.
Routing follows the spanning tree from a node to the gateway, and returns null for a node that is not connected.
Removing a node also removes its edges.

Together they check that the data structures return correct results for normal use, edge cases and invalid input.
<img width="1905" height="255" alt="Screenshot 2026-11-13 200910" src="https://github.com/user-attachments/assets/8d71c95d-164b-4405-9ec0-cfaa55044bac" />
