(function () {
    const messagesList = document.getElementById("messagesList");
    const connectionStatus = document.getElementById("connectionStatus");
    const userInfo = document.getElementById("userInfo");

    let messageCount = 0;

    function addMessage(type, data) {
        messageCount++;

        const messageDiv = document.createElement("div");
        messageDiv.className = "message-item";

        const headerDiv = document.createElement("div");
        headerDiv.className = "message-header";
        headerDiv.textContent = `#${messageCount} - ${type} - ${new Date().toLocaleTimeString()}`;

        const contentDiv = document.createElement("div");
        contentDiv.className = "message-content";
        contentDiv.textContent = JSON.stringify(data, null, 2);

        messageDiv.appendChild(headerDiv);
        messageDiv.appendChild(contentDiv);

        messagesList.insertBefore(messageDiv, messagesList.firstChild);

        // Keep only last 10 messages
        while (messagesList.children.length > 10) {
            messagesList.removeChild(messagesList.lastChild);
        }
    }

    function updateConnectionStatus(status, info = "") {
        connectionStatus.textContent = status;
        connectionStatus.className = status.toLowerCase().includes('connected') ? 'status-connected' : 'status-disconnected';
        //userInfo.textContent = info;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chathub")   // cookies/session-based auth
        .withAutomaticReconnect()
        .build();

    // Handle test data from TestPanel
    connection.on("Test", (payload) => {
        addMessage("Test Data Received", payload);
    });

    // Handle regular messages
    connection.on("ReceiveMessage", (user, message) => {
        addMessage("Message Received", { from: user, message: message });
    });

    // Handle pairing events
    connection.on("PairedWith", (partnerId) => {
        addMessage("Paired", { partnerId });
        updateConnectionStatus("Connected & Paired", `Paired with: ${partnerId}`);
    });

    connection.on("Unpaired", (formerPartner) => {
        addMessage("Unpaired", { formerPartner });
        updateConnectionStatus("Connected but Unpaired", "Waiting for new partner...");
    });

    connection.on("Waiting", () => {
        addMessage("Status Update", { status: "waiting for partner" });
        updateConnectionStatus("Connected but Waiting", "Looking for a partner...");
    });

    // Handle user list updates
    connection.on("UserListUpdated", (users) => {
        addMessage("User List Update", { onlineUsers: users, count: users.length });
    });

    // Connection events
    connection.onreconnecting(() => {
        updateConnectionStatus("Reconnecting...", "");
        addMessage("Connection Status", { status: "reconnecting" });
    });

    connection.onreconnected(() => {
        updateConnectionStatus("Reconnected", "");
        addMessage("Connection Status", { status: "reconnected" });
    });

    connection.onclose(() => {
        updateConnectionStatus("Disconnected", "");
        addMessage("Connection Status", { status: "disconnected" });
    });

    // Start connection
    updateConnectionStatus("Connecting...", "");
    connection.start()
        .then(() => connection.invoke("GetUserId"))
        .then(id => userInfo.textContent = id)
        .then(() => {
            updateConnectionStatus("Connected", "Waiting for events...");
            addMessage("Connection Status", { status: "connected and ready" });
        })
        .catch(err => {
            updateConnectionStatus("Connection Failed", "");
            addMessage("Connection Error", { error: err.message });
        });
})();