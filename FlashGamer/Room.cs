using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FlashGamer
{
    public class Room
    {
        public int maxCount;

        public Dictionary<string, bool> Members;
        public string Leader;
        public string thisPlayerID;
        public int count = 0;
        public string RoomCode;
        public bool SimpleRoom;
        public bool amready = false;

        public Timer tick;
        public readonly int tickMS = 80;
        public int MapOrGameCode;

        public byte[] TCPPayload;
        public byte[] UDPPayload;
        public bool HasMessage = false;

        public Room(int maxMembers, string code, bool isSimple, string makerID)
        {
            this.maxCount = maxMembers;
            this.RoomCode = code;
            tick = new Timer(tickMS);
            tick.Elapsed += NetworkTick_Elapsed;
            Members = new Dictionary<string, bool>(maxMembers);
            AddMember(makerID);
        }

        private void NetworkTick_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!HasMessage)
            {
                return;
            }

            //at the end
            HasMessage = false;
        }

        public bool AddMember(string Id)
        {
            if (Members.ContainsKey(Id))
            {
                // hacking ??
                return false;
            }

            if (maxCount == count)
            {
                return false;
            }
            else
            {
                lock (Members)
                {
                    Members.Add(Id, false);
                    count += 1;

                    if (count == 1)
                    {
                        Leader = Id;
                    }
                }

                return true;
            }

        }

        public bool RemoveMember(string Id)
        {
            if (count <= 0 || thisPlayerID != Leader || !Members.ContainsKey(Id))
            {
                return false;
            }

            lock (Members)
            {
                Members.Remove(Id);
                count -= 1;
            }
            return true;
        }

        public bool ProcessJoin(string Password, string id)
        {
            if (thisPlayerID != Leader)
            {
                // hacking ??
                return false;
            }

            if (SimpleRoom)
            {
                //todo demand input and process
                bool yes = AskAcceptJoin().Result;

                if (!yes)
                {
                    return false;
                }
                else
                {
                    return AddMember(id);
                }

            }
            else
            {
                if (Password != RoomCode)
                {
                    return false;
                }

                return AddMember(id);
            }

        }

        public void SendMessage(string message)
        {
            HasMessage = true;
        }

        public void RecordAudio() // use udp
        {
            HasMessage = true;
        }

        public void StartActivity() // start the game..
        {
            if (Leader != thisPlayerID)
            {
                //hacking ??
                return;
            }
        }

        public async Task<bool> AskAcceptJoin()
        {
            await Task.Delay(100);
            return true; // todo show UI.. for now
        }

        public void MarkReady()
        {
            if (SimpleRoom)
            {
                return;
            }

            else
            {
                amready = !amready;
            }
        }

        public bool ChangeLeader(string newID)
        {
            if (Members.ContainsKey(newID))
            {
                if (Leader == thisPlayerID)
                {
                    Leader = newID;
                    return true;
                }
                else
                {
                    //hacking?
                    return false;
                }
            }
            else
            {
                // hacking??
                return false;
            }
        }
    }
}
