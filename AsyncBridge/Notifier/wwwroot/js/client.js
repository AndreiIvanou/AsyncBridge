$(document).ready(function () {
    $("#sendButton").click(function () {
        var host = jobDispatcherHost;
        var url = "http://" + host + "/api/JobDispatcher";

        var jobName = $("#jobName").val();
        var jobParameters = $("#jobParameters").val();
        var request = { jobName: jobName, jobParameters: jobParameters };

        $.ajax({
            url: url,
            type: "Post",
            data: JSON.stringify(request),
            contentType: 'application/json; charset=utf-8',
            error: function (msg) {
                alert(msg);
            }
        }).done(function (data, status, req) {
            var jobId = req.getResponseHeader("Location");

            var li = document.createElement("li");
            li.textContent = "Job " + jobId + " was submitted at " + new Date() + ".";
            document.getElementById("messagesList").appendChild(li);

            var connection = new signalR.HubConnectionBuilder().withUrl("/NotificationHub").build();

            connection.on("ReceiveJobResult", function (jobResult) {
                var li = document.createElement("li");
                li.textContent = jobResult;
                document.getElementById("messagesList").appendChild(li);
            });

            connection.start()
            .then(function () {
                connection.invoke("SubscribeToJobStatusUpdates", jobId)
                    .catch(function (err) {
                        alert(err.toString());
                        return console.error(err.toString());
                    });
            })
            .catch(function (err) {
                return console.error(err.toString());
            });
        });
    });
});