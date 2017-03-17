return function (input, cb) {
    var login = require('facebook-chat-api');
    login({email: input.username, password: input.password}, function callback (err, api) {
        //console.log("login(): Err = " + err);
       
        input.onLoginFinish(err, function(error, result) {
            console.log("Running: onLoginFinish");
            //if (error) console.log('onLoginFinish(): ' + error);
        });
        if (err) {
            console.log(err);
            cb(null);
            return;
        }

        api.setOptions({ listenEvents: true});
        api.setOptions({ selfListen: false});

        api.listen(function(err, message) {
            console.log(message);
            if (message.type == 'message')
            {
                //api.sendMessage(message.body, message.threadID);
                input.onMessage(message, function(error, result) {  // message = System.ExpandoObject
                    if (error) console.log('error: ' + error);
                });
            }
        });
        console.log("Facebook event listener set");
        // TODO: Expose a function to .NET
        //cb();
        cb(err, function(data, cbb) {
            //console.log("js fbLoggedIn() Call");
            // Check request
            switch (data.request) {
                case "send":
                    api.sendMessage(data.body, data.threadID, function(error, messageInfo) {
                        //console.log(messageInfo);
                        //console.log("sendMessage() error: " + error);
                        data.callback(err, function(error, result) {
                            if (error) console.log('error: ' + error);
                        });
                    });
                    break;
                case "onlineUsers":
                    api.getOnlineUsers(function(err, arr) {
                        //console.log(arr);
                        data.callback(arr,  function(error, result) {
                            if (error) console.log('error: ' + error);
                        });
                    });
                    break;
                case "getThreadInfo":
                    api.getThreadInfo(data.threadID, function(error, info) {
                        if(error == null) {
                            //console.log(info);
                            data.callback(info, function(error, result) {
                                if (error) console.log('error: ' + error);
                            });
                        }
                    });
                    break;
                case "getCurrentID":
                    var id = api.getCurrentUserID();
                    data.callback(id, function(error, result) {
                        if (error) console.log('error: ' + error);
                    });
                    break;
                case "getUserInfo":
                    api.getUserInfo(data.userID, function(error, userInfo) {
                        //console.log(userInfo);
                        // TODO: Check error first, callback with either error or userInfo
                        data.callback(userInfo, function(error, result) {
                            if (error) console.log('error: ' + error);
                        });
                    });
                    break;
                case "getThreadHist":
                    api.getThreadHistory(data.threadID, data.start, data.end, null, function(error, history) {
                        //console.log(history);
                        data.callback(history, function(error, result) {
                            if (error) console.log('error: ' + error);
                        });
                    });
                    break;
                default:
                    ;
            }
            
            cbb();
        });
    });
    //cb();
};