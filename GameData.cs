using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpClient
{
    class GameData
    {
        protected Boolean m_fullyEnteredGame;
        public Boolean FullyEnteredGame
        {
            get { return m_fullyEnteredGame; }
            set { m_fullyEnteredGame = value; }
        }

        protected Int32 m_lastTeleport;
        public Int32 LastTeleport
        {
            get { return m_lastTeleport; }
            set { m_lastTeleport = value; }
        }

        protected Int32 m_experience;
        public Int32 Experience
        {
            get { return m_experience; }
            set { m_experience = value; }
        }

    }
}
