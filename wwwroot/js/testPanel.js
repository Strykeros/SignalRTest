// wwwroot/js/testPanel.js
(function () {
    const output = document.getElementById("output");
    const btn = document.getElementById("sendForm");
    const userIdInput = document.getElementById("userId");

    const log = (v) => (output.textContent = typeof v === "string" ? v : JSON.stringify(v, null, 2));

    function getRequestVerificationToken() {
        const el = document.querySelector('#af input[name="__RequestVerificationToken"]');
        return el ? el.value : "";
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chathub")
        .withAutomaticReconnect()
        .build();

    connection.on("Test", (payload) => {
        log({
            type: "Received Test Data",
            receivedAt: new Date().toISOString(),
            payload
        });
    });

    async function ensureConnected() {
        if (connection.state === signalR.HubConnectionState.Disconnected) {
            await connection.start();
        }
    }

    btn.addEventListener("click", async (e) => {
        e.preventDefault();
        const userId = userIdInput.value.trim();
        if (!userId) return log("Please enter a userId.");

        try {
            // 1) Tell server to set Session["UserId"] = userId
            const connectRes = await fetch("?handler=Connect", {
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                    "RequestVerificationToken": getRequestVerificationToken(),
                },
                body: new URLSearchParams({ userId }),
            });

            if (!connectRes.ok) {
                return log(`Failed to set user id: ${await connectRes.text()}`);
            }

            // 2) Restart (or start) the hub so it picks up the new session user id
            if (connection.state !== signalR.HubConnectionState.Disconnected) {
                await connection.stop();
            }
            await ensureConnected();
            log(`SignalR connected as ${userId}`);

            // 3) Send test data to the specified user
            const testRes = await fetch("?handler=Test", {
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                    "RequestVerificationToken": getRequestVerificationToken(),
                },
                body: new URLSearchParams({ userId }),
            });

            if (!testRes.ok) {
                return log(`Error ${testRes.status}: ${await testRes.text()}`);
            }

            const result = await testRes.json();
            log({
                type: "Test Data Sent Successfully",
                sentAt: new Date().toISOString(),
                result
            });

        } catch (err) {
            log("Request failed: " + err.message);
        }
    });

    // Initialize connection for receiving responses (optional)
    ensureConnected()
        .then(() => log("TestPanel ready to send test data"))
        .catch(err => log("SignalR connection failed (but can still send tests): " + err.message));
})();