using EdgeJs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Wpf_FB
{
    public interface fbAPI_Interface
    {
        void Start();
        FB_TheadInfo getThreadInfo(string threadID);
        List<FB_Message> getThreadHistory(FB_TheadInfo thread, int maxCount);
        bool sendMsg(string threadID, string body);
        Dictionary<string, FB_UserInfo> getFriendList();
    }

    public class FB_Message
    {
        public string senderID;
        public string threadID;
        public string body;
        public Int64 timestamp; // epoch timestamp 
        public bool isGroup;
        public string attachmentType;
        public object attachment;

        // Return human readable date
        public DateTime timestampToDate()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds(timestamp);
        }

        public string ToString()
        {
            string output;
            output = string.Format(@"senderID: {0}
treadID: {1}
body: {2}
timestamp: {3}
isGroup: {4}
attachmentType: {5}
attachment: {6}",
                                    senderID, threadID, body, timestamp, isGroup,
                                    attachmentType, attachment);
            return output;
        }
    }

    public class FB_Sticker
    {
        public string url;
        public string stickerID;
        public string packID;
        public int frameCount;
        public int frameRate;
        public int framesPerRow;
        public int framesPerCol;
        public string spriteURI;
        public string spriteURI2x;
        public int height;
        public int width;
        public string caption;
        public string description;

        public string ToString()
        {
            string output;
            output = string.Format(@"url: {0}
stickerID: {1}
packID: {2}
frameCount: {3}
frameRate: {4}
framePerRow: {5}
framePerCol: {6}
spriteURI: {7}
spriteURI2x: {8}
height: {9}
width: {10}
caption: {11}
description: {12}",
                                    url, stickerID, packID, frameCount, frameRate, framesPerRow,
                                    framesPerCol, spriteURI, spriteURI2x, height, width,
                                    caption, description);
            return output;
        }
    }

    public class FB_Request
    {
        public enum request_enum
        {
            SEND,
            GET_ONLINE_USERS,
            GET_CURRENT_ID,
            GET_USER_INFO,
            GET_THREAD_INFO,
            GET_THREAD_HIST
        }
        public FB_Request.request_enum request;
        public dynamic requestData = new ExpandoObject();

        // Field required in requestData:
        //   SEND: body, threadID
        //   GET_ONLINE_USERS: 
        //   GET_USER_INFO: userID
        //   GET_THREAD_INFO: threadID
    }

    public class FB_TheadInfo
    {
        public string threadID;
        public string name;
        public int messageCount;
    }

    public class FB_UserInfo
    {
        public string userID;
        public string name;
        public string firstName;
        public string profileUrl;
        public string thumbSrc;
        public string vanity;
        public bool isFriend;
        public bool isBirthday;
        public bool alternateName;
        public string status;
        public string type;
        public int gender;
        public Int64 lastActive;
    }

    public class fbAPI : fbAPI_Interface
    {
        // Shared variable for other pages to access
        // auto property
        public string username { get; set; }
        public string password { get; set; }
        public string currentUserID { get; set; }
        public int loginState { get; set; } = 0;    // 0=not logged in; 1=successful; -1=failed

        // Event related
        private ManualResetEvent loginDone = new ManualResetEvent(false);
        private ManualResetEvent sendDone = new ManualResetEvent(false);
        private ManualResetEvent getOnlineUserDone = new ManualResetEvent(false);
        private ManualResetEvent getUserInfoDone = new ManualResetEvent(false);
        private bool sendSuccess = false;
        
        

        // Events
        /*
        public delegate void msgRecevieHandler(fbAPI api, FB_Message e);
        public event msgRecevieHandler receiveMsgEvent;
        public static FB_Message e = new FB_Message();
        */

        // Collections
        // New message will be added into bMsgCollection whenever the api receives
        public BlockingCollection<FB_Message> bMsgCollection = new BlockingCollection<FB_Message>(100);

        public BlockingCollection<FB_Message> bHistoryCollection = new BlockingCollection<FB_Message>(1000);

        // API would keep monitoring bReqCollection collection and works based on the request received
        private BlockingCollection<FB_Request> bReqCollection = new BlockingCollection<FB_Request>(100);

        // A dictionary for storing a list of users
        // ket is userID
        private Dictionary<string, FB_UserInfo> dUserList = new Dictionary<string, FB_UserInfo>();

        // After requesting GET_THREAD_INFO, API will put the thread's name into this collection
        // GUI should wait the data after requsting GET_THREAD_INFO
        public BlockingCollection<FB_TheadInfo> bThreadInfoCollection = new BlockingCollection<FB_TheadInfo>(10);

        public fbAPI()
        {
            // Constructor
        }

        //public async void Start(string username, string password)
        public async void Start()
        {
            // Initialize the js funtion by Edge.js 
            var func = Edge.Func(File.ReadAllText("../../FB_ChatAPI.js"));

            /* Callback handlers for js callbacks */
            // First argument: received object from js
            // Second argument: Callback to js, pass an object back to js for its own callback function
            var onMessage = (Func<dynamic, Task<object>>)(async (message) =>
            {
                // message is ExpandoObject
                //Console.WriteLine("[C#]: {0}\n", message.threadID);

                // Put message into collection
                FB_Message msg = new FB_Message();
                msg.senderID = (string)message.senderID;
                msg.threadID = (string)message.threadID;
                msg.body = (string)message.body;
                msg.timestamp = Int64.Parse((string)message.timestamp);
                msg.isGroup = message.isGroup;
                // Check attachment (Only sticker is supported by now)
                //Console.WriteLine(message.attachments[0]);
                // Unsafe to check message.body == null for attachment
                // But don't know how to check if there is object inside attachment yet
                if (message.body == null && message.attachments[0].type == "sticker")
                {
                    FB_Sticker s = new FB_Sticker();
                    msg.attachmentType = "sticker";
                    s.url = (string)message.attachments[0].url;
                    s.stickerID = (string)message.attachments[0].stickerID;
                    s.packID = (string)message.attachments[0].packID;
                    s.frameCount = (int)message.attachments[0].frameCount;
                    s.frameRate = (int)message.attachments[0].frameRate;
                    s.framesPerRow = (int)message.attachments[0].framesPerRow;
                    s.framesPerCol = (int)message.attachments[0].framesPerCol;
                    s.spriteURI = (string)message.attachments[0].spriteURI;
                    s.spriteURI2x = (string)message.attachments[0].spriteURI2x;
                    s.height = (int)message.attachments[0].height;
                    s.width = (int)message.attachments[0].width;
                    s.caption = (string)message.attachments[0].caption;
                    s.description = (string)message.attachments[0].description;
                    msg.attachment = s;
                    //Console.WriteLine(msg.ToString());
                    //Console.WriteLine(s.ToString());
                }
                bMsgCollection.Add(msg);

                return message;
            });

            var onLoginFinish = (Func<dynamic, Task<object>>)(async (state) =>
            {
                // Clean the data
                username = string.Empty;
                password = string.Empty;
                if (state == null)   // state is null on success, an object otherwise
                {
                    Console.WriteLine("Login successful");
                    loginState = 1;
                    loginDone.Set();
                }
                else {
                    loginState = -1;
                    loginDone.Set();
                }
                return state;
            });

            var onOnlineUsersRetrieved = (Func<dynamic, Task<object>>)(async (onlineUsers) =>
            {
                //Console.WriteLine("Result: " + onlineUsers);
                dUserList.Clear();
                foreach(dynamic u in onlineUsers)
                {
                    string userID = u.userID;
                    string status = u.status;
                    
                    //Int64 lastActive = Int64.Parse((string)u.lastActive);
                    FB_UserInfo info = new FB_UserInfo();

                    //Console.WriteLine(userID);
                    //Console.WriteLine(status);
                    info.status = status;
                    dUserList.Add(userID, info);    // Add user to dictionary
                }
                getOnlineUserDone.Set();  // Signal task done
                return onlineUsers;
            });

            var onCurrentIDRetrieved = (Func<dynamic, Task<object>>)(async (userID) =>
            {
                currentUserID = userID;
                return userID;
            });

            var onUserInfoRetrieved = (Func<dynamic, Task<object>>) (async (userInfo) =>
            {
                // userInfo: Expandoobject
                //Console.WriteLine(userInfo);
                
                // Each userInfo is in KeyValuePair<string, dynamic>
                // value is Expandoobject
                foreach (KeyValuePair<string, dynamic> user in userInfo)
                {
                    string userID = user.Key;
                    dynamic userProp = user.Value;
                    //Console.WriteLine(user);
                    //Console.WriteLine(userProp);
                    if (dUserList.ContainsKey(user.Key))
                    {
                        // Update user list
                        dUserList[userID].name = userProp.name;
                        dUserList[userID].firstName = userProp.firstName;
                        dUserList[userID].isBirthday = userProp.isBirthday;
                        dUserList[userID].profileUrl = userProp.profileUrl;
                        dUserList[userID].type = userProp.type;
                        dUserList[userID].thumbSrc = userProp.thumbSrc;
                        dUserList[userID].vanity = userProp.vanity;
                        dUserList[userID].gender = userProp.gender;
                        dUserList[userID].userID = userID;
                        //dUserList[userID].alternateName = userProp.alternateName; // No such member
                    }

                    getUserInfoDone.Set();  // Notify job is done
                }
                
                return userInfo;
            });

            var onThreadInfoRetrieved = (Func<dynamic, Task<object>>)(async (info) =>
            {
                FB_TheadInfo thread = new FB_TheadInfo();
                string name = (string)info.name;
                int messageCount = (int)info.messageCount;
                //Console.WriteLine("Thread name: {0} {1}", info.name, info.messageCount);
                thread.name = name;
                thread.messageCount = messageCount;
                bThreadInfoCollection.Add(thread);
                return info;
            });

            var onMsgSent = (Func<dynamic, Task<object>>)(async (err) =>
           {
               if(err == null)
               {
                   sendSuccess = true;
               }
               else
               {
                   sendSuccess = false;
               }
               sendDone.Set();  // release lock
               return err;
           });

            var onThreadHistRetrieve = (Func<dynamic, Task<object>>)(async (hist) =>
            {
                FB_Message msg;

                // Put each message into collection
                foreach(dynamic h in hist)
                {
                    /*
                    Console.WriteLine(h.senderID);
                    Console.WriteLine(h.threadID);
                    Console.WriteLine(h.isGroup);
                    Console.WriteLine(h.body);
                    Console.WriteLine(h.senderID);
                    Console.WriteLine(h.timestamp);
                    */
                    msg = new FB_Message();
                    msg.senderID = (string)h.senderID;
                    msg.threadID = (string)h.threadID;
                    msg.isGroup = (bool)h.isGroup;
                    msg.body = (string)h.body;
                    msg.timestamp = (long)h.timestamp;  // h.timestamp is not string this time but a number
                    // TODO: Retrieve sticker as well
                    bHistoryCollection.Add(msg);
                }
                // Complete adding
                bHistoryCollection.CompleteAdding();
                
                return hist;
            });

            

            /* Main procedure starts here */
            try {
                // fbActiveState is a js function you can access after login
                var fbActiveState = (Func<dynamic, Task<object>>)await func(new
                {
                    username = username,
                    password = password,
                    onMessage = onMessage,
                    onLoginFinish = onLoginFinish
                }); // A node.js function is exposed to fbLoggedIn 

                // Return if login fail
                if(loginState == -1)
                {
                    Console.WriteLine("Start(): Login failed");
                    return;
                }

                // Logistics: Start() stay alive thorughout the life
                // Wait for a event call (GUI put request into bReqCollection)
                // Pass the data into js (await fbLoggedIn())
                // loop
                loginDone.WaitOne();
                Console.WriteLine("C#: Login Done");


                // Login done, get current user id and set on attribute
                await fbActiveState(new
                {
                    request = "getCurrentID",
                    callback = onCurrentIDRetrieved
                });

                // Then wait for request forever
                FB_Request request = new FB_Request();
                while (true)
                {
                    // Wait for request
                    //Console.WriteLine("Taking...");
                    request = bReqCollection.Take();

                    // Read request
                    if(request.request == FB_Request.request_enum.SEND)
                    {

                        //Console.WriteLine("Sending...");
                        var requestRes = await fbActiveState(new
                        {
                            request = "send",
                            body = (string)request.requestData.body,
                            threadID = (string)request.requestData.threadID,
                            callback = onMsgSent
                        });
                    }
                    else if(request.request == FB_Request.request_enum.GET_ONLINE_USERS)
                    {
                        var requestRes = await fbActiveState(new
                        {
                            request = "onlineUsers",
                            callback = onOnlineUsersRetrieved
                        });
                    }
                    else if(request.request == FB_Request.request_enum.GET_USER_INFO)
                    {
                        // TODO: check if requestData has required fields
                        await fbActiveState(new
                        {
                            request = "getUserInfo",
                            userID = request.requestData.userID,
                            callback = onUserInfoRetrieved
                        });
                    }
                    else if(request.request == FB_Request.request_enum.GET_THREAD_INFO)
                    {
                        await fbActiveState(new
                        {
                            request = "getThreadInfo",
                            threadID = request.requestData.threadID,
                            callback = onThreadInfoRetrieved 
                        });
                    }
                    else if(request.request == FB_Request.request_enum.GET_THREAD_HIST)
                    {
                        await fbActiveState(new
                        {
                            request = "getThreadHist",
                            threadID = request.requestData.threadID,
                            start = request.requestData.start,
                            end = request.requestData.end,
                            callback = onThreadHistRetrieve
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("func() Error: {0}", e.ToString());
                return;
            }
        }

        public FB_TheadInfo getThreadInfo(string threadID)
        {
            FB_TheadInfo threadInfo = new FB_TheadInfo();

            FB_Request req = new FB_Request();
            req.request = FB_Request.request_enum.GET_THREAD_INFO;
            req.requestData.threadID = threadID;
            bReqCollection.Add(req);
            if (bThreadInfoCollection.TryTake(out threadInfo, 1000))
            {
                threadInfo.threadID = threadID;
                return threadInfo;
            }
            else
            {
                return null;
            }
        }

        public List<FB_Message> getThreadHistory(FB_TheadInfo thread, int maxCount)
        {
            List<FB_Message> msgList = new List<FB_Message>();
            FB_Request req = new FB_Request();

            // Reset collection first
            bHistoryCollection = new BlockingCollection<FB_Message>(1000);
            // If maxCount exceed messageCount, retrieve all messages
            if (thread.messageCount < maxCount)
            {
                maxCount = thread.messageCount - 1;
            }

            req.request = FB_Request.request_enum.GET_THREAD_HIST;
            req.requestData.threadID = thread.threadID;
            req.requestData.start = thread.messageCount - maxCount - 1; // Mind the index
            req.requestData.end = thread.messageCount - 1;
            bReqCollection.Add(req);

            while(!bHistoryCollection.IsCompleted)
            {
                msgList.Add(bHistoryCollection.Take());
            }

            return msgList;
        }

        public bool sendMsg(string threadID, string body)
        {
            FB_Request req = new FB_Request();
            req.request = FB_Request.request_enum.SEND;
            req.requestData.threadID = threadID;
            req.requestData.body = body;

            // Lock thread and send message
            sendDone.Reset();
            bReqCollection.Add(req);
            sendDone.WaitOne();

            // Read send result
            return sendSuccess;
        }

        public Dictionary<string, FB_UserInfo> getFriendList()
        {
            FB_Request req = new FB_Request();
            getOnlineUserDone.Reset();    // Lock the task
            req.request = FB_Request.request_enum.GET_ONLINE_USERS;
            bReqCollection.Add(req);
            getOnlineUserDone.WaitOne();    // Wait for job done

            // Add all userIDs to a list for forwarding to get detailed user info
            List<string> list = new List<string>();
            foreach (KeyValuePair<string, FB_UserInfo> user in dUserList)
            {
                list.Add(user.Key);
            }

            getUserInfoDone.Reset();    // Lock the task
            req = new FB_Request();
            req.request = FB_Request.request_enum.GET_USER_INFO;
            req.requestData.userID = list;
            bReqCollection.Add(req);
            getUserInfoDone.WaitOne();  // Wait for job done
            return dUserList;
        }
    }
}
