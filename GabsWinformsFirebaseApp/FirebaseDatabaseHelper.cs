using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GabsWinformsFirebaseApp
{
    public class FirebaseDatabaseHelper
    {
        // https://github.com/step-up-labs/firebase-database-dotnet
        // https://stackoverflow.com/questions/37418372/firebase-where-is-my-account-secret-in-the-new-console

        private readonly FirebaseClient _firebaseClient;
        private readonly string _firebaseEndpoint;
        private readonly string _idPrefix;

        public bool IsConnected { get; } = false;

        public FirebaseDatabaseHelper(string firebaseApiKey, string firebaseDatabaseURL, string firebaseEndpoint)
        {
            _firebaseClient = new FirebaseClient(firebaseDatabaseURL,
              new FirebaseOptions
              {
                  AuthTokenAsyncFactory = () => Task.FromResult(firebaseApiKey)
              });

            IsConnected = _firebaseClient != null;
            _firebaseEndpoint = firebaseEndpoint;
            _idPrefix = _firebaseEndpoint.Substring(0, 1).ToLower();
        }

        public FirebaseClient GetClient()
        {
            return _firebaseClient;
        }

        public async Task<List<T>> GetAllAsync<T>()
        {
            var responseGet = await _firebaseClient.Child(_firebaseEndpoint).OnceAsync<T>();
            var result = responseGet.Select(s => s.Object).ToList();
            return result;
        }

        public async Task<T> GetAsync<T>(string id)
        {
            var responseGet = await _firebaseClient.Child(_firebaseEndpoint).Child(_idPrefix + id).OnceAsync<T>();
            var result = responseGet.Select(s => s.Object).FirstOrDefault();
            return result;
        }

        public async Task<T> SetAsync<T>(string id, object obj)
        {
            await _firebaseClient.Child(_firebaseEndpoint).Child(_idPrefix + id).PutAsync(obj);
            var result = (T)obj;
            return result;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                await _firebaseClient.Child(_firebaseEndpoint).Child(_idPrefix + id).DeleteAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> GenerateIdAsync()
        {
            int Id;

            var result = await _firebaseClient.Child("_Ids").Child(_firebaseEndpoint).OnceAsync<int>();
            if (result != null)
            {
                Id = result.Select(s => s.Object).FirstOrDefault();
                if (Id == 0) Id = 1;
            }
            else
                Id = 1;

            // Increment Id by 1
            var newIdGen = new FirebaseIdGenerator();
            newIdGen.Id = Id + 1;
            await _firebaseClient.Child("_Ids").Child(_firebaseEndpoint).PutAsync(newIdGen);

            return Id;
        }

        public void ListenEventStreaming<TSender, TObject>(object sender, string strSenderMethod = "ListenToFirebase")
        {
            var observable = _firebaseClient.Child(_firebaseEndpoint).AsObservable<TObject>().Subscribe(x =>
            {
                int eventType = x.EventType == Firebase.Database.Streaming.FirebaseEventType.Delete ? 1 : 0;
                object[] objParam = { eventType, x.Object };
                MethodInfo method = sender.GetType().GetMethod(strSenderMethod);
                method.Invoke(sender, objParam);
            });

        }


        private class FirebaseIdGenerator
        {
            public int Id { get; set; }
        }
    }

}
