using System;
using System.Globalization;
using System.Windows.Data;
using BO;
namespace DC2 {
	class ConverterIlosc : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			typ_ilość item = (typ_ilość)value;
			if (item == null) return "";
			if (item.jednostka == null) return item.Items.ToString();
			else return item.Items.ToString() + item.jednostka.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			float f;
			typ_ilość item = new typ_ilość();
			if (float.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out f)) {
				item.Items = f;
				item.jednostka = null;
			}
			else if (value.ToString() == "Nie dotyczy") {
				item.Items = "Nie dotyczy";
				item.jednostka = null;
			}
			else {
				bool isValid = false;
				int leng = value.ToString().Length;
				for (int i = leng - 1; i >= 0 && !isValid; i--) {
					if (float.TryParse(value.ToString().Substring(0, i), NumberStyles.Any, CultureInfo.InvariantCulture, out f)) {
						item.Items = f;
						
						item.jednostka = value.ToString().Substring(i, leng - i);
						isValid = true;

					}
				}
				if (!isValid) {
					item.Items = value.ToString();
					item.jednostka = null;
				}
			}
			return item;
		}
	}
}
