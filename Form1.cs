using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using PushbulletSharp;
using PushbulletSharp.Models.Requests;
using PushbulletSharp.Models.Responses;

namespace PingMiner
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            getText();
            //Console.WriteLine(pingNow());
        }

        private void getText()
        {
            string path = @"config.txt";

            // This text is added only once to the file.
            if (File.Exists(path))
            {
                string[] readText = File.ReadAllLines(path);
                //line 1 is the IP address
                if (readText.Length > 0) textBox1.Text = readText[0];
                //line 2 is the pushbullet access token
                if (readText.Length > 1) textBox2.Text = readText[1];
            }
        }

        private bool pingNow()
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(textBox1.Text);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }

            return pingable;
        }

        bool lastPing = true;
        bool twoPings = true;

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (pingNow())
            {
                lastPing = true;
                twoPings = true;
                this.Text = "OK " + DateTime.Now.ToString("HH:mm:ss");
            }
            else
            {
                sendSMS("MINER FAIL @ " + DateTime.Now.ToString("HH:mm:ss"));
                if (lastPing) //this is the first time failing
                {
                    lastPing = false;
                    twoPings = true;
                    this.Text = "1 Failure @ " + DateTime.Now.ToString("HH:mm:ss");
                }
                else
                {
                    if (twoPings) //this is the third or more fail in a row, assume the system crashed
                    {
                        //FAIL
                        timer2.Enabled = true;
                        this.Text = "FAILED @ " + DateTime.Now.ToString("HH:mm:ss");
                        sendSMS("MINER FAILED @ " + DateTime.Now.ToString("HH:mm:ss"));
                        LoopNotifications = true;
                        //MessageBox.Show("FAILURE DETECTED!");
                    }
                    else
                    {
                        twoPings = true; //next time if it fails it will be considered a problem
                        this.Text = "2 Failure @ " + DateTime.Now.ToString("HH:mm:ss");
                    }
                }
            }
        }

        bool LoopNotifications = false;
        int numNotifications = 0;
        string checkIP = "192.168.1.214";

        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            this.Text = "Restarted...";
            checkIP = textBox1.Text;
            timer1_Tick(sender, e);
        }


        //code from and more info at https://en.code-bude.net/2018/02/14/send-push-notifications-c/
        private void sendSMS(string messageBody)
        {
            //Create client
            var pushBulletApiKey = textBox2.Text;
            PushbulletClient client = new PushbulletClient(pushBulletApiKey);

            // This block sends to all users/devices attached to this account.
            //Fetch device information from account
            var devices = client.CurrentUsersDevices();

            //Select all Android devices
            var targetDevices = devices.Devices.Where(o => string.Equals(o.Kind, "android"));

            for (int i = 0; i < devices.Devices.Count; i++)
            {
                if (devices.Devices[i].Kind == "android")
                {
                    PushNoteRequest request = new PushNoteRequest
                    {
                        //LIMIT 65 characters for titles and 240 for descriptions
                        DeviceIden = devices.Devices[i].Iden,
                        Title = "---MINER ALERT---",
                        Body = messageBody // $"MINER FAILED"
                    };

                    PushResponse response = client.PushNote(request);
                }
            }

            ////Send notification to each Android device
            //foreach (var device in targetDevices)
            //{
            //    PushNoteRequest request = new PushNoteRequest
            //    {
            //        //LIMIT 65 characters for titles and 240 for descriptions
            //        DeviceIden = device.Iden,
            //        Title = "---Crypto Price Alert---",
            //        Body = $"Message for: {device.Model}\nTrying a second line to see how this works.\nBTC Exceeded 45230 Limit.\nTrying to go to all 240 characters and it's a lot longer than I expected. "
            //    };

            //    PushResponse response = client.PushNote(request);
            //}

            //This block only sends to one user, whoever is the default. I chose the above instead in case it has problems when I get a new phone.
            ////Get information about the user account behind the API key
            //var currentUserInformation = client.CurrentUsersInformation();

            ////Check if useraccount data could be retrieved
            //if (currentUserInformation != null)
            //{
            //    //Create request
            //    PushNoteRequest request = new PushNoteRequest
            //    {
            //        Email = currentUserInformation.Email,
            //        Title = "This is the headline",
            //        Body = "Here comes the text."
            //    };

            //    PushResponse response = client.PushNote(request);
            //}
        }

        private void button2_Click(object sender, EventArgs e)
        {
            sendSMS("TESTING");
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (!LoopNotifications) return;

            sendSMS("Miner failed-reminder");
            numNotifications++;
            if (numNotifications > 20)
            {
                LoopNotifications = false;
                timer1.Enabled = false;
                timer2.Enabled = false;
            }
        }
    }
}
