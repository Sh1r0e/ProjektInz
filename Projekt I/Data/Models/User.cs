using Google.Cloud.Firestore;

namespace Projekt_I.Data.Models
{
    [FirestoreData]
    public class User
    {
        [FirestoreProperty]
        public string Id { get; set; } // Firestore-generated document ID
        [FirestoreProperty]
        public string Username { get; set; }
    }
}
