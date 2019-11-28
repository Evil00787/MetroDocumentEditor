using BO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace DC2 {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	/// 



	public partial class MainWindow : MetroWindow {
		bool isOpened = false;
		string chosenFile = "";
		typ_formularza now_editing = new typ_formularza();
		public ObservableCollection<typ_koszt> kosztList = new ObservableCollection<typ_koszt>();
		private static object _syncLock = new object();
		bool hasUnsavedChanges = false;
		string path = "";
		Dictionary<string, bool> validation = new Dictionary<string, bool>();
		public List<Dictionary<string, bool>> tabVal = new List<Dictionary<string, bool>>();
		int lastEditedIndex = 0;
		bool loadedFromFile = false;
		int unsavedChanges = 0;
		DriveService service;

		static string[] Scopes = { DriveService.Scope.DriveReadonly };
		static string ApplicationName = "Drive API .NET Quickstart";

		IList<Google.Apis.Drive.v3.Data.File> googleFiles;

		private void LoadDialog() {
			var fileContent = string.Empty;
			var filePath = string.Empty;


			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";

			if (openFileDialog.ShowDialog() == true) {
				//Get the path of specified file
				LoadFileFromPath(openFileDialog.FileName);

			}
		}


		private void LoadFileFromPath(string filePath) {
			if (filePath != null) {
				loadedFromFile = true;
				path = filePath;
				CleanForm();
				ClearDict();
				Validation(filePath);
				chosenFile = filePath;
				typ_formularza formularz = DeserializeObject(filePath);
				LoadToFormularz(formularz, Path.GetFileNameWithoutExtension(filePath));
			}
		}


		private void LoadToFormularz(typ_formularza formularz, string name) {
			now_editing = formularz;
			DataContext = now_editing;
			//imienazwisko.Text = formularz.Imie_nazwisko;
			//msc.Text = formularz.Adres.Miejscowosc_ulica;
			//kod.Text = formularz.Adres.Kod_pocztowy;
			//dom.Text = formularz.Adres.Nr_domu;
			//tel.Text = formularz.Kontakt.Nr_telefonu;
			//email.Text = formularz.Kontakt.Email;
			//mieszk.Text = formularz.Adres.Nr_mieszkania;
			//opis.Text = formularz.Opis;
			//skropis.Text = formularz.Skrót_opisu;
			//lok.Text = formularz.Lokalizacja;
			//tyt.Text = formularz.Tytuł;
			//zakres.SelectedItem = formularz.Własność_terenu;
			Task.Factory.StartNew(() => {
				if (formularz.Koszta_projektu.Items != null) {
					foreach (var item in formularz.Koszta_projektu.Items) {
						lock (_syncLock) {
							kosztList.Add(item);
						}


					}
				}

			});

			//Read the contents of the file into a stream
			Flyout.Header = name;
			OpenFlyout();
			enableBack();
			setNoUnsavedChanges();
		}


		private void OpenFlyout() {
			Flyout.IsOpen = true;
		}

		private typ_formularza DeserializeObject(string filename) {

			XmlSerializer serializer = new XmlSerializer(typeof(typ_formularza));

			// Declare an object variable of the type to be deserialized.
			typ_formularza i;

			using (Stream reader = new FileStream(filename, FileMode.Open)) {
				// Call the Deserialize method to restore the object's state.
				try {
					i = (typ_formularza)serializer.Deserialize(reader);
				}
				catch {
					i = new typ_formularza();
					i = new typ_formularza();
					i.Adres = new typ_adres();
					i.Kontakt = new typ_kontakt();
					i.Koszta_projektu = new typ_koszta_projektu();
					OpenDialogErrorReading();
				}
			}

			// Write out the properties of the object.
			return i;
		}

		private typ_formularza DeserializeObject(Stream stream) {

			XmlSerializer serializer = new XmlSerializer(typeof(typ_formularza));

			// Declare an object variable of the type to be deserialized.
			typ_formularza i;


			// Call the Deserialize method to restore the object's state.
			try {
				i = (typ_formularza)serializer.Deserialize(stream);
			}
			catch {
				i = new typ_formularza();
				i = new typ_formularza();
				i.Adres = new typ_adres();
				i.Kontakt = new typ_kontakt();
				i.Koszta_projektu = new typ_koszta_projektu();
				OpenDialogErrorReading();
			}


			// Write out the properties of the object.
			return i;
		}

		private bool SerializeObject(string filename) {
			Console.WriteLine("Writing With TextWriter");

			XmlSerializer serializer =
			new XmlSerializer(typeof(typ_formularza));
			typ_formularza formularz = new typ_formularza();
			formularz.Imie_nazwisko = now_editing.Imie_nazwisko;
			formularz.Adres = now_editing.Adres;
			formularz.Kontakt = now_editing.Kontakt;
			formularz.Opis = now_editing.Opis;
			formularz.Skrót_opisu = now_editing.Skrót_opisu;
			formularz.Lokalizacja = now_editing.Lokalizacja;
			formularz.Tytuł = now_editing.Tytuł;
			formularz.Własność_terenu = now_editing.Własność_terenu;
			formularz.Koszta_projektu = new typ_koszta_projektu();
			formularz.Koszta_projektu.Items = new typ_koszt[kosztList.Count];



			int i = 0;
			foreach (typ_koszt k in kosztList) {
				formularz.Koszta_projektu.Items[i] = k;
				i++;
			}

			/* Create a StreamWriter to write with. First create a FileStream
			   object, and create the StreamWriter specifying an Encoding to use. */
			using (FileStream fs = new FileStream(filename, FileMode.Create)) {
				using (TextWriter writer = new StreamWriter(fs, new UTF8Encoding())) {
					try { serializer.Serialize(writer, formularz); }
					catch {
						Console.WriteLine("Error writing");
					}
				}
			}

			return true;
		}

		void enableBack() {
			if (!isOpened) {
				isOpened = true;
				BackArrowButton.Visibility = Visibility.Visible;
			}

		}

		void CleanForm() {
			Task.Factory.StartNew(() => {
				lock (_syncLock) {
					kosztList.Clear();
				}

			});

			now_editing = new typ_formularza();
			now_editing.Adres = new typ_adres();
			now_editing.Kontakt = new typ_kontakt();
			now_editing.Koszta_projektu = new typ_koszta_projektu();




			now_editing.Imie_nazwisko = "";
			now_editing.Lokalizacja = "";
			now_editing.Skrót_opisu = "";
			now_editing.Opis = "";
			now_editing.Tytuł = "";
			now_editing.Własność_terenu = typ_wlasnosc_terenu.gminy;
			now_editing.Kontakt.Email = "";
			now_editing.Kontakt.Nr_telefonu = "";
			now_editing.Adres.Kod_pocztowy = "";
			now_editing.Adres.Miejscowosc_ulica = "";
			now_editing.Adres.Nr_domu = "";
			now_editing.Adres.Nr_mieszkania = "";


			imienazwisko.Text = "";
			msc.Text = "";
			kod.Text = "";
			dom.Text = "";
			tel.Text = "";
			email.Text = "";
			mieszk.Text = "";
			opis.Text = "";
			skropis.Text = "";
			lok.Text = "";
			tyt.Text = "";
			zakres.SelectedItem = typ_wlasnosc_terenu.gminy;

			tabVal.Clear();
			validation.Clear();

		}




		static void ValidationEventHandler(object sender, ValidationEventArgs e) {
			switch (e.Severity) {
				case XmlSeverityType.Error:
					Console.WriteLine("Error: {0}", e.Message);
					break;
				case XmlSeverityType.Warning:
					Console.WriteLine("Warning {0}", e.Message);
					break;
			}

		}

		private bool Validation(string filename) {
			try {
				XmlReaderSettings settings = new XmlReaderSettings();
				settings.Schemas.Add("http://www.mat.com/budzet", "budżet_obywatelski.xsd");
				settings.ValidationType = ValidationType.Schema;

				using (XmlReader reader = XmlReader.Create(filename, settings)) {
					XmlDocument document = new XmlDocument();
					document.Load(reader);

					ValidationEventHandler eventHandler = new ValidationEventHandler(ValidationEventHandler);

					// the following call to Validate succeeds.
					document.Validate(eventHandler);
					return true;
				}
			}
			catch (Exception ex) {
				Console.WriteLine(ex.Message);
				OpenDialogErrorReading();
				return false;
			}

		}

		private bool Validation(Stream stream) {
			try {
				XmlReaderSettings settings = new XmlReaderSettings();
				settings.Schemas.Add("http://www.mat.com/budzet", "budżet_obywatelski.xsd");
				settings.ValidationType = ValidationType.Schema;

				using (XmlReader reader = XmlReader.Create(stream, settings)) {
					XmlDocument document = new XmlDocument();
					document.Load(reader);

					ValidationEventHandler eventHandler = new ValidationEventHandler(ValidationEventHandler);

					// the following call to Validate succeeds.
					document.Validate(eventHandler);
					return true;
				}
			}

			catch (Exception ex) {
				Console.WriteLine(ex.Message);
				OpenDialogErrorReading();
				return false;
			}

		}


		public MainWindow() {


			InitializeComponent();
			Flyout.IsModal = false;
			BindingOperations.EnableCollectionSynchronization(kosztList, _syncLock);
			tabela.ItemsSource = kosztList;

			DataContext = now_editing;

			CleanForm();
			typ_formularza.isValid = (valid, name) => changeValidationDict(name, valid);
			typ_kontakt.isValid = (valid, name) => changeValidationDict(name, valid);
			typ_koszt.isValid = (valid, name) => changeValidationTab(lastEditedIndex, name, valid);
			typ_adres.isValid = (valid, name) => changeValidationDict(name, valid);
			typ_ilość.isValid = (valid, name) => changeValidationDict(name, valid);


			kosztList.CollectionChanged += OnCollectionChanged;
			tabela.CellEditEnding += Tabela_CellEditEnding;




			UserCredential credential;

			using (var stream =
				new FileStream("credentials.json", FileMode.Open, FileAccess.Read)) {
				// The file token.json stores the user's access and refresh tokens, and is created
				// automatically when the authorization flow completes for the first time.
				string credPath = "token.json";
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					Scopes,
					"user",
					CancellationToken.None,
					new FileDataStore(credPath, true)).Result;
				Console.WriteLine("Credential file saved to: " + credPath);
			}

			// Create Drive API service.
			service = new DriveService(new BaseClientService.Initializer() {
				HttpClientInitializer = credential,
				ApplicationName = ApplicationName,
			});
		}

		private IList<Google.Apis.Drive.v3.Data.File> getListOfFiles() {
			FilesResource.ListRequest listRequest = service.Files.List();
			listRequest.PageSize = 100;
			listRequest.Fields = "nextPageToken, files(id, name)";

			IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;
			return files;
		}


		private void Tabela_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) {

			lastEditedIndex = e.Row.GetIndex();
		}

		void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (e.Action == NotifyCollectionChangedAction.Add) {
				if (loadedFromFile) {
					insertValidationTabFromFile();
				}
				else {
					insertValidationTab();
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove) {
				deleteValidationTab(e.OldStartingIndex);
			}
		}



		private void ClearDict() {
			validation.Clear();
			ClearTabVal();
		}

		private void ClearTabVal() {
			foreach (var a in tabVal) {
				a.Clear();
			}
			tabVal.Clear();
		}

		private void changeValidationTab(int index, string name, bool valid) {
			if (index >= 0) {
				tabVal[index][name] = valid;
			}
		}

		private void insertValidationTab() {
			tabVal.Add(new Dictionary<string, bool>());
			tabVal[tabVal.Count - 1].Add("Nazwa", false);
			tabVal[tabVal.Count - 1].Add("Miejsce", false);
			tabVal[tabVal.Count - 1].Add("kwota", true);
		}

		private void insertValidationTabFromFile() {
			tabVal.Add(new Dictionary<string, bool>());
			tabVal[tabVal.Count - 1].Add("Nazwa", true);
			tabVal[tabVal.Count - 1].Add("Miejsce", true);
			tabVal[tabVal.Count - 1].Add("Ilość", true);
			tabVal[tabVal.Count - 1].Add("kwota", true);
		}

		private void deleteValidationTab(int index) {
			tabVal.RemoveAt(index);
		}

		private void changeValidationDict(string name, bool valid) {
			if (validation.ContainsKey(name)) {
				validation[name] = valid;
			}
			else {
				validation.Add(name, valid);
			}
			setHasUnsavedChanges();
		}

		private bool checkValidation() {
			foreach (var a in validation) {
				if (!a.Value) { return false; }
			}
			foreach (var dict in tabVal) {
				foreach (var name in dict) {
					if (!name.Value) { return false; }
				}
			}
			return true;
		}

		private void setNoUnsavedChanges() {
			hasUnsavedChanges = false;
			unsavedChanges = 0;
		}

		private void setHasUnsavedChanges() {

			if (unsavedChanges > 12) {
				hasUnsavedChanges = true;
			}
			else {
				unsavedChanges++;
			}
		}

		private void Load_File_Click(object sender, RoutedEventArgs e) {
			if (hasUnsavedChanges) {
				Open_Dialog_Unsaved(false);
			}
			else {
				LoadDialog();
			}
		}




		private void Create_File_Click(object sender, RoutedEventArgs e) {
			if (hasUnsavedChanges) {
				Open_Dialog_Unsaved(true);
			}
			else {
				CleanForm();
			}
			setNoUnsavedChanges();
			path = "";
			Flyout.Header = "New XML";
			OpenFlyout();
		}

		private void MetroWindow_Loaded(object sender, RoutedEventArgs e) {

		}

		private void Open_Flyout(object sender, RoutedEventArgs e) {
			OpenFlyout();
		}

		private void Save_File() {
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "XML file (*.xml)|*.xml";
			if (saveFileDialog.ShowDialog() == true) {

				Open_Dialog_Saving();
				SerializeObject(saveFileDialog.FileName);
				setNoUnsavedChanges();
				Flyout.Header = Path.GetFileNameWithoutExtension(saveFileDialog.FileName).ToString();
			}
		}

		private async void Open_Dialog_Unsaved(bool isNew) {
			MetroDialogSettings options = new MetroDialogSettings();
			options.FirstAuxiliaryButtonText = "Save";
			options.DefaultButtonFocus = MessageDialogResult.FirstAuxiliary;
			options.AffirmativeButtonText = "No";
			MessageDialogResult result = await this.ShowMessageAsync("Do you want to save changes?", "You will loose all your progress if you click No.", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, options);
			if (result == MessageDialogResult.Affirmative) {
				if (isNew) {
					CleanForm();
				}
				else {
					LoadDialog();
				}
			}
			else if (result == MessageDialogResult.FirstAuxiliary) {
				Save_File();
				if (isNew) {
					CleanForm();
				}
				else {
					LoadDialog();
				}
			}
		}

		private async void OpenDialogNoValidation() {
			MetroDialogSettings options = new MetroDialogSettings();
			MessageDialogResult result = await this.ShowMessageAsync("Document is not validating", "You have to correct red boxes", MessageDialogStyle.Affirmative, options);

		}

		private async void OpenDialogErrorReading() {
			MetroDialogSettings options = new MetroDialogSettings();
			MessageDialogResult result = await this.ShowMessageAsync("Loaded document is invalid", "Displaying all readable fields", MessageDialogStyle.Affirmative, options);

		}

		private void SaveAs_Click(object sender, RoutedEventArgs e) {
			if (checkValidation()) {
				Save_File();
			}
			else {
				OpenDialogNoValidation();
			}
		}


		private async void Open_Dialog_Saving() {
			MetroDialogSettings options = new MetroDialogSettings();
			options.AnimateShow = false;

			var controller = await this.ShowProgressAsync("Please wait...", "", false, options);
			ProgressCircle.IsActive = true;
			await controller.CloseAsync();
			ProgressCircle.IsActive = false;
		}

		private void Tabela_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e) {
			string headername = e.Column.Header.ToString();
			if (headername == "Error") {
				e.Cancel = true;
			}
			if (headername != "Kategoria") {
				DataGridTextColumn column = (DataGridTextColumn)e.Column;
				Binding binding = (Binding)column.Binding;

				binding.ValidationRules.Add(new DataErrorValidationRule());
				binding.ValidationRules[0].ValidatesOnTargetUpdated = true;
				if (headername == "Ilość") {
					binding.Converter = new ConverterIlosc();
				}
			}


		}

		private void Tabela_AutoGeneratedColumns(object sender, EventArgs e) {
			var grid = (DataGrid)sender;
			foreach (var item in grid.Columns) {
				if (item.Header.ToString() == "Delete") {
					item.DisplayIndex = grid.Columns.Count - 1;
					break;
				}
			}
		}

		private void MetroWindow_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			maingrid.Focus();
		}

		private void Save_Click(object sender, RoutedEventArgs e) {

			if (checkValidation()) {
				if (path != "") {
					Open_Dialog_Saving();
					SerializeObject(path);
					setNoUnsavedChanges();
				}
				else {
					Save_File();
				}
			}
			else {
				OpenDialogNoValidation();
			}
		}

		private void Tabela_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e) {

		}

		private void RowAdd_Click(object sender, RoutedEventArgs e) {
			tabela.SelectedItem = null;
			loadedFromFile = false;
			typ_koszt adding = new typ_koszt();
			adding.Ilość = new typ_ilość();
			adding.Ilość.Items = 0;
			adding.kwota = "";
			adding.Miejsce = "";
			adding.Nazwa = "";
			lastEditedIndex = kosztList.Count;
			kosztList.Add(adding);

		}

		private void Drive_Click(object sender, RoutedEventArgs e) {
			Drive_flyout.IsOpen = true;
			googleFiles = getListOfFiles();
			int i = 0;
			foreach (var one in googleFiles) {
				addTile(i, one.Name);
				i++;
			}
		}

		private void addTile(int i, string name) {
			Tile tile = new Tile();
			tile.Name = "Tile" + i.ToString();
			tile.Title = name.Substring(0, name.Length - 4);
			tile.HorizontalAlignment = HorizontalAlignment.Left;
			tile.Width = 150;
			tile.Height = 150;
			tile.Margin = new Thickness(2.5, 16, 2.5, 16);
			tile.Click += onCloudFileClick;
			ContainerOfTiles.Children.Add(tile);
		}

		private void onCloudFileClick(object sender, RoutedEventArgs e) {
			Tile tile = sender as Tile;
			int index;
			if(int.TryParse(tile.Name.Substring(tile.Name.Length - 1, 1), out index)) {
				var file = googleFiles[index];
				MemoryStream memo = new MemoryStream();
				FilesResource.GetRequest getRequest = service.Files.Get(file.Id);

				getRequest.Download(memo);
				memo.Seek(0, SeekOrigin.Begin);
				loadedFromFile = true;
				path = "cloud_" + file.Name;
				CleanForm();
				ClearDict();
				Validation(memo);
				chosenFile = "cloud_" + file.Name;
				memo.Seek(0, SeekOrigin.Begin);
				typ_formularza formularz = DeserializeObject(memo);
				LoadToFormularz(formularz, "cloud: " + Path.GetFileNameWithoutExtension(file.Name));
			}
			
		}

		private void ScrolViewFlyout_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) {
			ScrollViewer scrollviewer = sender as ScrollViewer;
			if (e.Delta > 0) {
				scrollviewer.LineLeft();
			}
			else {
				scrollviewer.LineRight();
			}

			e.Handled = true;
		}
	}
}



