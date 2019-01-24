/*
 * Created by SharpDevelop.
 * User: Gabs
 * Date: 24/01/2019
 * Time: 11:52 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GabsWinformsFirebaseApp
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

        // https://stackoverflow.com/questions/37418372/firebase-where-is-my-account-secret-in-the-new-console
        private string firebaseApiKey = "NYMI0Ko6jsOJRfXh0aGlBEUnd8Z5mwt9iHaeYXV9";
        private string firebaseDatabaseURL = "https://gabsfirebasewinapp.firebaseio.com";

        private IFirebaseClient _firebaseClient;
        private string _firebaseEndpoint = "Cars";
        private List<CarType> carTypes = new List<CarType>();

        private void MainForm_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;

            // https://github.com/ziyasal/FireSharp
            IFirebaseConfig config = new FirebaseConfig();
            config.AuthSecret = firebaseApiKey;
            config.BasePath = firebaseDatabaseURL;
            _firebaseClient = new FirebaseClient(config);
            if (_firebaseClient != null)
            {
                MessageBox.Show("Connected to Firebase Realtime Database.");
                GetCarsFromFirebase();
                ListenToFirebase();
            }
            else
                MessageBox.Show("Error connecting to Firebase.");

            LoadInitialCarTypes();

        }

        private async void GetCarsFromFirebase()
        {
            if (_firebaseClient != null)
            {
                FirebaseResponse responseGet = await _firebaseClient.GetAsync(_firebaseEndpoint);
                var result = responseGet.ResultAs<Dictionary<string, Car>>();
                if (result != null)
                {
                    try
                    {
                        var cars = result.Select(s => s.Value).ToList(); 
                        bindingSourceCar.DataSource = cars;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                else
                {
                    Car car1 = new Car();
                    car1.Id = 1;
                    car1.Brand = "Toyota";
                    car1.Model = "Wigo";
                    car1.Type = "1";
                    car1.Color = "Red";
                    var responseSet = await _firebaseClient.SetAsync(_firebaseEndpoint + "/c" + car1.Id.ToString(), car1);
                    var res = responseSet.ResultAs<Car>();
                    await GetNewCarId();
                    GetCarsFromFirebase();
                }
            }

        }

        private void LoadInitialCarTypes()
        {
            var carType1 = new CarType();
            carType1.Id = "1";
            carType1.Type = "Hatchback";
            carTypes.Add(carType1);

            var carType2 = new CarType();
            carType2.Id = "2";
            carType2.Type = "SUV";
            carTypes.Add(carType2);

            comboCarType.DataSource = carTypes;
            comboCarType.DisplayMember = "Type";
            comboCarType.ValueMember = "Id";
        }

        private async Task<int> GetNewCarId()
        {
            int carId = 1;

            var responseGet = await _firebaseClient.GetAsync("_Ids/" + _firebaseEndpoint);
            var result = responseGet.ResultAs<CarIdGenerator>();
            if (result != null)
                carId = result.Id;

            // Increment Id by 1
            var newCarIdGen = new CarIdGenerator();
            newCarIdGen.Id = carId + 1;
            await _firebaseClient.SetAsync("_Ids/" + _firebaseEndpoint, newCarIdGen);

            return carId;
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            btnSave.Enabled = false;
            string btnSaveOrigText = btnSave.Text;
            btnSave.Text = "Saving...";

            Car newCar = bindingSourceCar.Current as Car;
            if (newCar != null)
            {
                if (string.IsNullOrWhiteSpace(txtCarId.Text) || txtCarId.Text == "0")
                    newCar.Id = await GetNewCarId();

                var responseSet = await _firebaseClient.SetAsync(_firebaseEndpoint + "/c" + newCar.Id.ToString(), newCar);
                var result = responseSet.ResultAs<Car>();
                if (result != null)
                {
                    txtCarId.Text = result.Id.ToString();
                    bindingSourceCar.EndEdit();
                    MessageBox.Show("Successfully saved to Firebase.");
                    btnSave.Text = btnSaveOrigText;
                    btnSave.Enabled = true;
                    txtCarId.Focus();
                }
                else
                    MessageBox.Show("Error saving the data to Firebase.");
            }
        }

        private async void bindingNavigatorDeleteItem_MouseDown(object sender, MouseEventArgs e)
        {
            var delCar = bindingSourceCar.Current as Car;
            var responseDel = await _firebaseClient.DeleteAsync(_firebaseEndpoint + "/c" + delCar.Id);
            if (responseDel.StatusCode != System.Net.HttpStatusCode.OK)
                MessageBox.Show("Deletion failed.");
        }

        private async void ListenToFirebase()
        {
            EventStreamResponse response = await _firebaseClient.OnAsync(_firebaseEndpoint,
            added: (s, args, d) =>
            {
                var id = args.Path.Split('/')[1];
                firebaseStatus.Text = "[" + DateTime.Now.ToString("hh:mm:ss:fff tt") + "] [{id}] Received ADDED updates from Firebase...";
            },
            changed: (s, args, d) =>
            {
                var id = args.Path.Split('/')[1];
                firebaseStatus.Text = "[" + DateTime.Now.ToString("hh:mm:ss:fff tt") + "] [{id}] Received CHANGED updates from Firebase...";
            },
            removed: (s, args, d) =>
            {
                var id = args.Path.Split('/')[1];
                firebaseStatus.Text = "[" + DateTime.Now.ToString("hh:mm:ss:fff tt") + "] [{id}] Received DELETE updates from Firebase...";
            });
        }
    }
}
