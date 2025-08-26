// Build SignalR connection to your ChatHub endpoint
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

// UI elements
const sendButton = document.getElementById("sendButton");
const connectButton = document.getElementById("connectButton"); // NEW
const userInput = document.getElementById("userInput");
const messageInput = document.getElementById("messageInput");
const messagesList = document.getElementById("messagesList");
const usersList = document.getElementById("usersList");         // NEW

// Disable send until connected
sendButton.disabled = true;
if (connectButton) connectButton.disabled = true;

// Helpers
function addMessage(text) {
    const li = document.createElement("li");
    li.textContent = text;
    messagesList.appendChild(li);
}

function renderUsers(users) {
    if (!usersList) return;
    usersList.innerHTML = "";
    users.forEach(u => {
        const li = document.createElement("li");
        li.textContent = u;
        li.style.cursor = "pointer";
        li.title = "Click to target this user";
        li.addEventListener("click", () => {
            userInput.value = u; // quick-pick into the input
        });
        usersList.appendChild(li);
    });
}

// Receive a message from server
connection.on("ReceiveMessage", (fromUser, message) => {
    const who = fromUser ?? "(unknown)";
    addMessage(`${who}: ${message}`);
});

// Receive updated user list from server
connection.on("UserListUpdated", (users) => {
    renderUsers(users || []);
});

// Start connection
async function start() {
    try {
        await connection.start();
        sendButton.disabled = false;
        if (connectButton) connectButton.disabled = false;
        addMessage("Connected.");
        // Optional: fetch users immediately if you want
        // const users = await connection.invoke("GetConnectedUsers");
        // renderUsers(users || []);
    } catch (err) {
        console.error(err);
        addMessage("Connection failed, retrying in 5s...");
        setTimeout(start, 5000);
    }
}
start();

// Connect/pair with the selected user
if (connectButton) {
    connectButton.addEventListener("click", async () => {
        const target = userInput.value.trim();
        if (!target) {
            alert("Pick a user to connect with (click a user in the list).");
            return;
        }
        try {
            const ok = await connection.invoke("ConnectWith", target);
            addMessage(ok
                ? `(system) Pairing created with ${target}.`
                : `(system) You are already paired with ${target}.`);
        } catch (err) {
            console.error(err);
            addMessage("Error creating pairing.");
        }
    });
}

// Send on Enter in message box
messageInput.addEventListener("keydown", (e) => {
    if (e.key === "Enter") {
        sendButton.click();
    }
});

connection.on("Waiting", () => {
    addMessage("(system) Waiting for someone to pair with...");
});

connection.on("PairedWith", (otherUser) => {
    addMessage(`(system) You are now paired with ${otherUser}.`);
    // Default the target to your partner so the Send button just works
    const userInput = document.getElementById("userInput");
    if (userInput) userInput.value = otherUser;
});

connection.on("Unpaired", (leftUser) => {
    addMessage(`(system) ${leftUser} left. Re-matching you...`);
});

// On connect
async function start() {
    try {
        await connection.start();
        sendButton.disabled = false;
        addMessage("Connected.");
    } catch (err) {
        console.error(err);
        if (connectButton.disabled === false) {
            addMessage("Connection failed, retrying in 5s...");
        }

        setTimeout(start, 5000);
    }
}
start();

// Send button: if no explicit target, send to current partner
sendButton.addEventListener("click", async () => {
    const user = userInput.value.trim();
    const msg = messageInput.value.trim();
    if (!msg) return;
    try {
        if (user) {
            await connection.invoke("SendMessage", user, msg);
            addMessage(`(to ${user}) ${msg}`);
        } else {
            await connection.invoke("SendToPartner", msg);
            addMessage(`(to partner) ${msg}`);
        }
        messageInput.value = "";
        messageInput.focus();
    } catch (err) {
        console.error(err);
        addMessage("Error sending message.");
    }
});

