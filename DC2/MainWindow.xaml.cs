using BO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using HtmlAgilityPack;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
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

		string[] scopes = new string[] { DriveService.Scope.Drive };
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

		private bool SerializeObjectDrive(string filename) {
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

			string mimetype = "text/xml";
			/* Create a StreamWriter to write with. First create a FileStream
			   object, and create the StreamWriter specifying an Encoding to use. */
			using (MemoryStream fs = new MemoryStream()) {
				using (TextWriter writer = new StreamWriter(fs, new UTF8Encoding())) {
					try {
						serializer.Serialize(writer, formularz);
						fs.Flush();
						FilesResource.CreateMediaUpload createMU;
						var uploadedFile = new Google.Apis.Drive.v3.Data.File();
						uploadedFile.Name = filename + ".xml";
						uploadedFile.MimeType = mimetype;
						createMU = service.Files.Create(uploadedFile, fs, mimetype);
						createMU.Fields = "id";
						try {
							var x = createMU.Upload();
							var remoteFile = createMU.ResponseBody;
							Console.WriteLine("File uploaded successfully (File ID: " + remoteFile.Id + ")");
							return true;
						}
						catch (Exception e) {
							Console.WriteLine("File could not be uploaded (" + e.Message + ")");
							return false;
						}
					}
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
					scopes,
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
				float suma = 0;
				foreach (var item in kosztList) {
					float temp;
					if (float.TryParse(item.kwota, NumberStyles.Any, CultureInfo.InvariantCulture, out temp)) {
						suma += temp;
					}
				}
				output.Text = "Suma: " + suma.ToString();

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
			if (name == "kwota") {
				float suma = 0;
				foreach (var item in kosztList) {
					float temp;
					if (float.TryParse(item.kwota, NumberStyles.Any, CultureInfo.InvariantCulture, out temp)) {
						suma += temp;
					}
				}
				output.Text = "Suma: " + suma.ToString();
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
				if (name == "Kod_pocztowy" && valid) {
					parseHTML(now_editing.Adres.Kod_pocztowy);
				}
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
			ContainerOfTiles.Children.Clear();
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
			if (int.TryParse(tile.Name.Substring(tile.Name.Length - 1, 1), out index)) {
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
				Drive_flyout.IsOpen = false;
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

		private void CloudUpload_Click(object sender, RoutedEventArgs e) {
			if (checkValidation()) {
				openFIleNameDIalog();
			}
			else {
				OpenDialogNoValidation();
			}
		}

		private async void openFIleNameDIalog() {
			MetroDialogSettings dialogSettings = new MetroDialogSettings();
			string inputDialog = await this.ShowInputAsync("Enter filename", "Filename:", dialogSettings);
			if (inputDialog != null && inputDialog != "") {
				Open_Dialog_Saving();
				SerializeObjectDrive(inputDialog);
				setNoUnsavedChanges();
				Flyout.Header = "cloud: " + Path.GetFileNameWithoutExtension(inputDialog).ToString();
			}

		}

		private void parseHTML(string kod) {
			string final_address = "https://znajdzkodpocztowy.pl/szukaj/mk-" + kod + "/";
			WebClient webc = new WebClient(); //Create a web client to fetch the HTML string
			HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
			doc.Load(webc.OpenRead(final_address), Encoding.UTF8);

			//Select the whole table with data (use XPath expressions basing on the tags from the HTML source)  
			HtmlNode mainDiv = doc.DocumentNode.SelectSingleNode("//html/body/div[1]/div[6]/div[2]/div[1]/div/h2//text()[normalize-space()]");

			if (mainDiv != null) {
				string text = mainDiv.InnerHtml;
				if (now_editing.Adres.Miejscowosc_ulica.Contains(",")) {
					string[] first = now_editing.Adres.Miejscowosc_ulica.Split(',');
					first[0] = text;
					now_editing.Adres.Miejscowosc_ulica = first[0] + "," + first[1];
				}
				else {
					now_editing.Adres.Miejscowosc_ulica = text + ", " + now_editing.Adres.Miejscowosc_ulica;
				}
			}
		}

		private void PdfSave_Click(object sender, RoutedEventArgs e) {
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "PDF document (*.pdf)|*.pdf";
			if (saveFileDialog.ShowDialog() == true) {
				using (FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write, FileShare.Read)) {
					Document document = new Document(PageSize.A4);
					PdfWriter writer = PdfWriter.GetInstance(document, fs);
					document.Open();
					document.NewPage();
					string normal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

					//Create a base font object making sure to specify IDENTITY-H
					BaseFont bf = BaseFont.CreateFont(normal, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
					Font normal_font = new Font(bf, 12, Font.NORMAL);

					Font bold = new Font(bf, 12, Font.BOLD);

					Font italic = new Font(bf, 12, Font.ITALIC);
					Paragraph element = new Paragraph("Formularz zgłoszeniowy propozycji projektu do Budżetu Obywatelskiego", bold);
					element.Alignment = Element.ALIGN_CENTER;
					document.Add(element);

					element = new Paragraph(" - ", normal_font);
					element.Alignment = Element.ALIGN_CENTER;
					document.Add(element);

					PdfPTable table = new PdfPTable(7);
					document.Add(new Paragraph("", normal_font));
					PdfPCell cell = new PdfPCell(new Phrase("Imię i  Nazwisko Wnioskodawcy", bold));
					table.AddCell(cell);
					cell = new PdfPCell(new Phrase(now_editing.Imie_nazwisko, normal_font));
					cell.Colspan = 6;
					table.AddCell(cell);

					cell = new PdfPCell(new Phrase("Adres zamieszkania", bold));
					table.AddCell(cell);
					cell = new PdfPCell(new Phrase("Miejscowość i ulica: " + now_editing.Adres.Miejscowosc_ulica + " Kod pocztowy: " + now_editing.Adres.Kod_pocztowy + " Nr domu: " + now_editing.Adres.Nr_domu + " Nr mieszkania: " + now_editing.Adres.Nr_mieszkania, normal_font));
					cell.Colspan = 6;
					table.AddCell(cell);

					cell = new PdfPCell(new Phrase("Kontakt z wnioskodawcą", bold));
					cell.Rowspan = 2;
					table.AddCell(cell);
					cell = new PdfPCell(new Phrase("Nr tel.: " + now_editing.Kontakt.Nr_telefonu, normal_font));
					cell.Colspan = 6;
					table.AddCell(cell);
					cell = new PdfPCell(new Phrase("e-mail: " + now_editing.Kontakt.Email, normal_font));
					cell.Colspan = 6;
					table.AddCell(cell);

					cell = new PdfPCell(new Phrase("1. Tytuł projektu (będzie zamieszczony na stronie internetowej).", bold));
					cell.Colspan = 7;
					table.AddCell(cell);
					cell = new PdfPCell(new Phrase(now_editing.Tytuł, normal_font));
					cell.Colspan = 7;
					table.AddCell(cell);

					cell = new PdfPCell(new Phrase("2. Lokalizacja miejsca realizacji projektu (prosimy wskazać miejsce realizacji zadania oraz nr działki, jeżeli jest to możliwe podać adres lub opisać obszar w sposób, który umożliwi jego identyfikację; w celach pomocniczych zasadne jest dołączenie mapy lub rysunku sytuacyjnego danego obszaru).", bold));
					cell.Colspan = 7;
					table.AddCell(cell);
					cell = new PdfPCell(new Phrase(now_editing.Lokalizacja, normal_font));
					cell.Colspan = 7;
					table.AddCell(cell);

					cell = new PdfPCell(new Phrase("3. Jaki jest stan własnościowy terenu, na którym ma być zlokalizowany projekt (prosimy wpisać np. czy jest to teren: gminny, wspólnoty mieszkaniowej, Skarbu Państwa, spółdzielni mieszkaniowej).", bold));
					cell.Colspan = 7;
					table.AddCell(cell);
					cell = new PdfPCell(new Phrase(now_editing.Własność_terenu.ToString(), normal_font));
					cell.Colspan = 7;
					table.AddCell(cell);

					cell = new PdfPCell(new Phrase("4. Opis projektu (prosimy opisać czego dotyczy projekt, co dokładnie ma być zrealizowane w ramach projektu).", bold));
					cell.Colspan = 7;
					table.AddCell(cell);
					cell = new PdfPCell(new Phrase(now_editing.Opis, normal_font));
					cell.Colspan = 7;
					table.AddCell(cell);

					cell = new PdfPCell(new Phrase("5. Skrócony opis projektu (prosimy opisać w max. 6 zdaniach zasadnicze informacje o projekcie. Ten skrócony opis będzie publikowany na stronie Budżetu Obywatelskiego na etapie działań informacyjnych i głosowania).", bold));
					cell.Colspan = 7;
					table.AddCell(cell);
					cell = new PdfPCell(new Phrase(now_editing.Skrót_opisu, normal_font));
					cell.Colspan = 7;
					table.AddCell(cell);

					cell = new PdfPCell(new Phrase("6. Szacunkowy koszt projektu (prosimy uwzględnić wszystkie możliwe składowe części zadania oraz ich szacunkowe koszty. Podanie kosztu szacunkowego jest obligatoryjne)", bold));
					cell.Colspan = 7;
					table.AddCell(cell);

					cell = new PdfPCell(new Phrase("Składowe części zadania", italic));
					cell.VerticalAlignment = Element.ALIGN_CENTER;
					cell.HorizontalAlignment = Element.ALIGN_CENTER;
					cell.Colspan = 3;
					table.AddCell(cell);
					cell = new PdfPCell(new Phrase("Kategoria kosztu", italic));
					cell.VerticalAlignment = Element.ALIGN_CENTER;
					cell.HorizontalAlignment = Element.ALIGN_CENTER;
					table.AddCell(cell);
					cell = new PdfPCell(new Phrase("Adres realizacji podzadania (lub “nie dotyczy”)", italic));
					cell.VerticalAlignment = Element.ALIGN_CENTER;
					cell.HorizontalAlignment = Element.ALIGN_CENTER;
					table.AddCell(cell);
					cell = new PdfPCell(new Phrase("Ilość i jednostka (lub “nie dotyczy”)", italic));
					cell.VerticalAlignment = Element.ALIGN_CENTER;
					cell.HorizontalAlignment = Element.ALIGN_CENTER;
					table.AddCell(cell);
					cell = new PdfPCell(new Phrase("Koszt", italic));
					cell.VerticalAlignment = Element.ALIGN_CENTER;
					cell.HorizontalAlignment = Element.ALIGN_CENTER;
					table.AddCell(cell);
					int i = 0;
					float suma = 0;

					foreach (var value in kosztList) {
						cell = new PdfPCell(new Phrase(i + ") " + value.Nazwa, normal_font));
						cell.Colspan = 3;
						table.AddCell(cell);
						cell = new PdfPCell(new Phrase(value.Kategoria.ToString(), normal_font));
						table.AddCell(cell);
						cell = new PdfPCell(new Phrase(value.Miejsce, normal_font));
						table.AddCell(cell);
						if (value.Ilość.jednostka != null) {
							cell = new PdfPCell(new Phrase(value.Ilość.Items.ToString() + value.Ilość.jednostka.ToString(), normal_font));
						}
						else {
							cell = new PdfPCell(new Phrase(value.Ilość.Items.ToString(), normal_font));
						}

						table.AddCell(cell);
						cell = new PdfPCell(new Phrase(value.kwota, normal_font));
						table.AddCell(cell);
						i++;
						float temp;
						if (float.TryParse(value.kwota, NumberStyles.Any, CultureInfo.InvariantCulture, out temp)) {
							suma += temp;
						}
					}

					cell = new PdfPCell(new Phrase("Łącznie szacunkowo:", bold));
					cell.VerticalAlignment = Element.ALIGN_CENTER;
					cell.HorizontalAlignment = Element.ALIGN_RIGHT;
					cell.Colspan = 6;
					table.AddCell(cell);
					cell = new PdfPCell(new Phrase(suma.ToString(), normal_font));
					table.AddCell(cell);


					document.Add(table);
					document.Close();
				}
			}
		}
	}
}




