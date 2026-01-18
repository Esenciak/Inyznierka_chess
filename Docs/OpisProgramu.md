# Opis programu Chess_inz

## Cel projektu
Chess_inz to gra oparta o Unity, która łączy planszową walkę figur z etapem przygotowania armii. Gracz przechodzi przez pętlę: sklep/rozstawienie figur → walka → nagrody → kolejna runda. Gra obsługuje tryb singleplayer i multiplayer.

## Sceny i przepływ rozgrywki
- **MainMenu**: ekran wejściowy. Po zakończeniu serii rund wyświetlany jest baner zwycięzcy.
- **Shop**: faza przygotowania. Gracz rozstawia swoje figury na planszy gracza i w ekwipunku, kupuje nowe figury oraz zapisuje układ przed startem bitwy.
- **Battle**: faza walki. Obie strony wykonują ruchy naprzemiennie, a wynik bitwy aktualizuje postępy.

Przepływ:
1. W scenie **Shop** gracz ustawia figurami i uruchamia start bitwy.
2. Układ jest zapisywany i ładowany w **Battle**.
3. Po zakończeniu walki postęp rundy jest zapisywany i następuje powrót do **Shop**.

## Plansza i figury
- Plansza składa się z trzech obszarów: gracza, środka i przeciwnika.
- Figury mają typy (np. Król, Pion, Hetman) oraz właściciela (Player/Enemy).
- Układ armii jest zapisywany w strukturach danych (lista `SavedPieceData`) zamiast bezpośrednio w obiektach sceny.
- Jeżeli armia gracza nie ma Króla, system dodaje go automatycznie w centrum planszy gracza podczas inicjalizacji ekwipunku.

## Ekwipunek
- Ekwipunek to osobna siatka pól po prawej stronie planszy gracza.
- Figury mogą być przenoszone pomiędzy planszą a ekwipunkiem.
- Układ ekwipunku jest zapisywany i odtwarzany oddzielnie od układu planszy.

## Walka i tury
- Ruchy są dozwolone tylko w odpowiedniej fazie (Placement/Battle).
- W singleplayerze tury przełączane są lokalnie, a ruch przeciwnika generuje AI.
- W multiplayerze tura jest kontrolowana przez serwer i synchronizowana przez Netcode.

## Postęp gry
- Po każdej rundzie aktualizowane są wygrane, porażki i liczba monet.
- Po serii rund (domyślnie 9) pojawia się podsumowanie i powrót do MainMenu.

## Multiplayer i połączenia sieciowe
- Multiplayer korzysta z Unity Lobby oraz Unity Relay.
- Host tworzy lobby i alokację Relay, a gracze dołączają przez kod sesji.
- Relay domyślnie wybiera region automatycznie. Aby wymusić konkretny region (np. europejski), można ustawić pole `relayRegionOverride` w inspektorze `LobbyMenu`.

## Najważniejsze skrypty
- `GameManager`: sterowanie fazami gry, turami i końcem rundy.
- `ShopManager`: zapis/odczyt układu planszy i ekwipunku oraz start bitwy.
- `BoardManager`: generowanie plansz i dostęp do pól.
- `InventoryManager`: obsługa ekwipunku i upewnianie się, że Król jest obecny.
- `LobbyMenu`: logowanie do usług Unity, tworzenie lobby i konfiguracja Relay.
