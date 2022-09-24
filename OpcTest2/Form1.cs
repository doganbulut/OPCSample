using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OpcTest2
{
    public partial class Form1 : Form
    {
        Opc.Da.Server server = null;
        OpcCom.Factory fact = new OpcCom.Factory();
        Opc.Da.Item[] items;
        Opc.Da.Subscription group;
        Opc.IRequest req;
        Opc.Da.WriteCompleteEventHandler WriteEventHandler;
        Opc.Da.ReadCompleteEventHandler ReadEventHandler;

        public Form1()
        {
            InitializeComponent();
        }

        private void GetOpcServers()
        {
            try
            {
                OpcCom.ServerEnumerator se = new OpcCom.ServerEnumerator();

                Opc.Server[] servers = se.GetAvailableServers(Opc.Specification.COM_DA_20);

                ListServers(servers);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ListServers(Opc.Server[] OpsServerList)
        {
            trwServers.Nodes.Clear();
            lstUrlList.Items.Clear();
            foreach (Opc.Server serv in OpsServerList)
            {
                TreeNode trn = new TreeNode(serv.Name);
                trn.Nodes.Add(serv.Url.HostName + " : " + serv.Url.Path + " : " + serv.Url.Port);
                trn.Nodes.Add(serv.Url.ToString());
                trn.Nodes.Add(serv.IsConnected.ToString());
                trwServers.Nodes.Add(trn);
                lstUrlList.Items.Add(serv.Url.ToString());
            }
        }


        private bool ConnectOPCServer(string OpcUrl)
        {
            // Create a server object and connect to the TwinCATOpcServer
            Opc.URL url = new Opc.URL(OpcUrl);
            
            server = new Opc.Da.Server(fact, null);

            try
            {
                server.Connect(url, new Opc.ConnectData(new System.Net.NetworkCredential()));
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
                return false;
            }

            // Okuma Grubu 
            Opc.Da.SubscriptionState groupState = new Opc.Da.SubscriptionState();
            groupState.Name = "Group1";
            groupState.Active = true;


            group = (Opc.Da.Subscription)server.CreateSubscription(groupState);
            group.DataChanged += new Opc.Da.DataChangedEventHandler(group_DataChanged);


            // Gruba 3 adet OPC değişkeni ekle
            items = new Opc.Da.Item[3];
            items[0] = new Opc.Da.Item();
            items[0].ItemName = "Bucket Brigade.Int1";
            items[1] = new Opc.Da.Item();
            items[1].ItemName = "Bucket Brigade.Int2";
            items[2] = new Opc.Da.Item();
            items[2].ItemName = "Bucket Brigade.Int3";
            items = group.AddItems(items);

            //Write Callback fonksiyonumuzu tutacak handler 
            WriteEventHandler = new Opc.Da.WriteCompleteEventHandler(WriteCompleteCallback);
             //Read Callback fonksiyonumuzu tutacak handler 
            ReadEventHandler = new Opc.Da.ReadCompleteEventHandler(ReadCompleteCallback);

            return true;
        }

        void group_DataChanged(object subscriptionHandle, object requestHandle, Opc.Da.ItemValueResult[] values)
        {
            foreach (Opc.Da.ItemValueResult chitem in values)
            {
                WriteLogList(chitem.ItemName + " : " +
                                chitem.ItemPath + " : " +
                                chitem.Key + " : " +
                                chitem.Value);
            }
        }


        void WriteLogList(string item)
        {
            lstLog.BeginInvoke((MethodInvoker)delegate
            {
                lstLog.Items.Insert(0, item);
            });
        }

        void WriteCompleteCallback(object clientHandle, Opc.IdentifiedResult[] results)
        {

            WriteLogList("Write completed");
            foreach (Opc.IdentifiedResult writeResult in results)
            {
                WriteLogList(writeResult.ItemName + " : " + writeResult.ResultID);
            }

        }

        void ReadCompleteCallback(object clientHandle, Opc.Da.ItemValueResult[] results)
        {
            WriteLogList("Read completed");
            foreach (Opc.Da.ItemValueResult readResult in results)
            {
                WriteLogList(readResult.ItemName + " : " + readResult.Value);
            }
            Console.WriteLine();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GetOpcServers();   
        }

        

        private void button4_Click(object sender, EventArgs e)
        {
            if (server.IsConnected)
            {
                MessageBox.Show("connected");
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if ((server != null) && (server.IsConnected))
                server.Disconnect();

            if (lstUrlList.SelectedItems.Count != 0)
                ConnectOPCServer(lstUrlList.SelectedItem.ToString());
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            Opc.Da.ItemValue[] iv = new Opc.Da.ItemValue[group.Items.Count()];
            Random rnd = new Random();


            for (int i = 0; i < group.Items.Count(); i++)
            {
                Opc.Da.ItemValue iiv = new Opc.Da.ItemValue(group.Items[i].ItemName);
                iiv.ItemPath = group.Items[i].ItemPath;
                iiv.ServerHandle = group.Items[i].ServerHandle;
                iiv.Value = rnd.Next(1,1000);
                iv[i] = iiv;
            }

            //group.Write(iv);
            //Asenkron
            group.Write(iv, 1234, WriteCompleteCallback, out req);
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            //group.Read(group.Items);
            //and now read the items again
            group.Read(group.Items, 123, ReadCompleteCallback, out req);
        }

        

       
    }
}