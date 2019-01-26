using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GabsWinformsFirebaseApp
{
    public class FireSharpHelper
    {
        // https://github.com/ziyasal/FireSharp
        // https://stackoverflow.com/questions/37418372/firebase-where-is-my-account-secret-in-the-new-console

        private readonly IFirebaseClient _firebaseClient;
        private readonly string _firebaseEndpoint;
        private readonly string _idPrefix;

        public bool IsConnected { get; } = false;

        public FireSharpHelper(string firebaseApiKey, string firebaseDatabaseURL, string firebaseEndpoint)
        {
            IFirebaseConfig config = new FirebaseConfig
            {
                AuthSecret = firebaseApiKey,
                BasePath = firebaseDatabaseURL
            };
            _firebaseClient = new FirebaseClient(config);
            IsConnected = _firebaseClient != null;
            _firebaseEndpoint = firebaseEndpoint;
            _idPrefix = _firebaseEndpoint.Substring(0, 1).ToLower();
        }

        public IFirebaseClient GetClient()
        {
            return _firebaseClient;
        }

        public async Task<List<T>> GetAllAsync<T>()
        {
            FirebaseResponse responseGet = await _firebaseClient.GetAsync(_firebaseEndpoint);
            var result = responseGet.ResultAs<Dictionary<string, T>>();
            if (result != null)
                return result.Select(s => s.Value).ToList();
            else
                return null;
        }

        public async Task<T> GetAsync<T>(string id)
        {
            var responseGet = await _firebaseClient.GetAsync(_firebaseEndpoint + "/" + _idPrefix + id);
            var result = responseGet.ResultAs<T>();
            return result;
        }

        public async Task<T> SetAsync<T>(string id, object obj)
        {
            var responseSet = await _firebaseClient.SetAsync(_firebaseEndpoint + "/" + _idPrefix + id, (T) obj);
            var result = responseSet.ResultAs<T>();
            return result;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var responseDel = await _firebaseClient.DeleteAsync(_firebaseEndpoint + "/" + _idPrefix + id);
            if (responseDel.StatusCode == System.Net.HttpStatusCode.OK)
                return true;
            else
                return false;
        }

        public async Task<int> GenerateIdAsync()
        {
            int Id;

            var responseGet = await _firebaseClient.GetAsync("_Ids/" + _firebaseEndpoint);
            var result = responseGet.ResultAs<FirebaseIdGenerator>();
            if (result != null)
            {
                Id = result.Id;
                if (Id == 0) Id = 1;
            }
            else
                Id = 1;

            // Increment Id by 1
            var newCarIdGen = new FirebaseIdGenerator();
            newCarIdGen.Id = Id + 1;
            await _firebaseClient.SetAsync("_Ids/" + _firebaseEndpoint, newCarIdGen);

            return Id;
        }

        private async void ListenToFirebase()
        {
            EventStreamResponse response = await _firebaseClient.OnAsync(_firebaseEndpoint,
            added: (s, args, d) =>
            {
                var id = args.Path.Split('/')[1];
                //form.firebaseStatus.Text = "[" + DateTime.Now.ToString("hh:mm:ss:fff tt") + "] [{id}] Received ADDED updates from Firebase...";
            },
            changed: (s, args, d) =>
            {
                var id = args.Path.Split('/')[1];
                //firebaseStatus.Text = "[" + DateTime.Now.ToString("hh:mm:ss:fff tt") + "] [{id}] Received CHANGED updates from Firebase...";
            },
            removed: (s, args, d) =>
            {
                var id = args.Path.Split('/')[1];
                //firebaseStatus.Text = "[" + DateTime.Now.ToString("hh:mm:ss:fff tt") + "] [{id}] Received DELETE updates from Firebase...";
            });
        }


        private class FirebaseIdGenerator
        {
            public int Id { get; set; }
        }
    }

}
