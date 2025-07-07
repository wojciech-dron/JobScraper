JobScraper - aplikacja do zarządzania ofertami oraz aplikacjami poszczególne oferty (lepszy excel)
Mam nadzieję że język angielski nie będzie przeszkodą :p

Główne funkcjonalności to:
- wersja portable, czyli można ją uruchamiać na komputerze bez instalowania dodatkowych programów i z każdego folderu z pomocą pliku .bat
- pobieranie ofert z podanych linków (pracuj.pl, rocketjobs.pl olx.pl, oraz pierwotnie serwisy dla programistów: Indeed, Justoin.It, NoFluffJobs) na podstawie linków do ofert z filtrami
- strona z ofertami pracy
- ręczne dodawanie/modyfikacja ofert
- widok ze szczegółami oraz wprowadzanie informacji dotyczących aplikacji na konkretną ofertę
- info o aplikacji do danej firmy (przydatne, jeśli było kilka ofert z tej samej firmy)
- strona z aplikacji użytkownika (filtrowanie, sortowanie oraz odrzucanie)
- konfiguracja źródeł

Uwagi:
- może następować duże zużycie zasobów ponieważ w odpalana jest przeglądarka do pobierania ofert (można ją odpalać w trybie ukrytym - "ShowBrowserWhenScraping": false w scraperSettings.json)
- zapisane oferty i konfiguracja znajdują się w folderze Data
- aby zaktualizować program należy wypakować nową wersję oraz przekopiować do niej folder Data z poprzedniej wersji
- aktualizacja aplikacji jest bezpieczna, nie usunie ona danych z poprzedniej wersji,
  natomiast nie zadziała przeniesienie danych z nowszej wersji do starszej
- aby usunąć wszystkie dane należy usunąć folder Data

Link do kodu źródłowego: https://github.com/wojciech-dron/JobScraper

Daj znać jak oceniasz apkę, odczucia oraz co jest do poprawy, docenię wszelki feedback ;)
