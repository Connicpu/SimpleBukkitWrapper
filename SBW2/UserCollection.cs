using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SBW2
{
    public class UserCollection : ICollection<UserItem>
    {
        public static UserCollection Get
        {
            get
            {
                return new UserCollection();
            }
        }

        public IEnumerator<UserItem> GetEnumerator()
        {
            return new Enumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(UserItem item)
        {
            Config.UserCache[item.Username] = "0";
        }

        /// <summary>
        /// DOES NOTHING! YOU CANNOT CLEAR THE USERDB!
        /// This is a blank implementation with no code.
        /// </summary>
        public void Clear()
        {
        }

        public bool Contains(UserItem item)
        {
            return !String.IsNullOrWhiteSpace(Config.UserCache[item.Username]);
        }

        public void CopyTo(UserItem[] array, int arrayIndex)
        {
            foreach (var username in UsersInCache)
            {
                array[arrayIndex] = new UserItem(username);
                ++arrayIndex;
            }
        }

        public bool Remove(UserItem item)
        {
            Config.UserCache[item.Username] = "-1";
            return true;
        }

        public int Count
        {
            get { return UsersInCache.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        private static List<String> UsersInCache
        {
            get
            {
                return Config.UserCache.Keys.Where(key => !key.EndsWith("§hash") && !key.EndsWith("§custom")
                    && Config.UserCache[key] != "-1").ToList();
            }
        }

        private class Enumerator : IEnumerator<UserItem>
        {
            private int i = -1;
            private string[] users = UsersInCache.ToArray();

            public void Dispose()
            {
                users = null;
            }

            public bool MoveNext()
            {
                if (++i < users.Length)
                {
                    return true;
                }
                i = -1;
                return false;
            }

            public void Reset()
            {
                i = -1;
            }

            public UserItem Current
            {
                get { return new UserItem(users[i]); }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }
    }

    public class UserItem
    {
        public UserItem(string user)
        {
            Username = user;
        }

        public string Username { get; private set; }

        public int SecurityLevel
        {
            get
            {
                int sec;
                return int.TryParse(Config.UserCache[Username], out sec) ? sec : 0;
            }
            set { Config.UserCache[Username] = value.ToString(CultureInfo.InvariantCulture); }
        }

        public string PasswordHash
        {
            get { return Config.UserCache[Username + "§hash"]; }
            set { Config.UserCache[Username + "§hash"] = value; }
        }

        public bool IsCustom
        {
            get { return Config.UserCache[Username + "§custom"] == "true"; }
            set { Config.UserCache[Username + "§custom"] = value.ToString(CultureInfo.InvariantCulture).ToLower(); }
        }
    }
}
