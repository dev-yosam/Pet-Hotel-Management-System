Pet Hotel Management System

This app manages owners, pets, room bookings, stays and daily care.
It has a React frontend, an ASP.NET Core API and a PostgreSQL database.

What you need

Install the .NET 9 SDK, Node.js 22.12 or later, and PostgreSQL 17.
Node.js 24 was used during development.
Start the PostgreSQL service before running the app.
Windows and macOS use the same commands below.
On Windows, use PowerShell. On macOS, use Terminal.

Database setup

Open a terminal in the folder that contains PetHotelManagement.sln.
Run:

dotnet restore
dotnet tool restore

Set your own local connection:

dotnet user-secrets set "ConnectionStrings:PetHotel" "Host=localhost;Port=5432;Database=pet_hotel;Username=postgres;Password=YOUR_PASSWORD" --project backend/PetHotel.Api

Replace YOUR_PASSWORD with your PostgreSQL password.
Change the host, port or username if your setup is different.
Keep this connection in user secrets. Do not put your password in the project files.
Each computer has its own connection and database.

Create the database and tables:

dotnet ef database update --project backend/PetHotel.Api -- --environment Development

The database user needs permission to create a database.
You can also create an empty pet_hotel database in pgAdmin first.

Start the backend

dotnet run --project backend/PetHotel.Api --launch-profile http

The API runs at http://localhost:5041.
Leave this terminal open.

Start the frontend

Open a second terminal in the project folder:

cd frontend
npm ci
npm run dev

Open the URL shown in the terminal, usually http://localhost:5173.
The frontend sends API requests to port 5041.
Both the frontend and backend must stay running.
Press Ctrl+C in each terminal to stop them.

Using the app

Open Pet Check-in and create an owner.
Register a pet for that owner.
The app has six Standard rooms (101-106) and six Deluxe rooms (201-206).
Rooms are set up automatically when the backend starts.
Choose planned arrival and departure times, then find an available room.
Create the booking and click Check in when the booked period has started.
For an immediate check-in, choose an arrival time a few minutes before now.
The departure time must still be in the future.

Open Daily Care to add tasks for the checked-in pet.
You can complete a task or delete an unfinished task.
Tasks become read-only after check-out.

Open Dashboard to view current stays and today's statistics.
Open Stay History to search bookings and stays.
Use View details to check in an existing booking or check out a stay.
Check-out saves the final charge.

Dates and times use Australia/Sydney.
Stay History filters use the planned arrival date, including both selected dates.
Dashboard arrivals and departures use actual check-in and check-out times.

Standard rooms cost AUD 40 per night.
Deluxe rooms cost AUD 60 per night, with a 10 percent discount for 7 nights or more.
Charges use the booked dates in Sydney, with a minimum of one night.

Build and test

From the project folder:

dotnet build PetHotelManagement.sln
dotnet test PetHotelManagement.sln
cd frontend
npm run build
npm run lint

Tests use PostgreSQL and the connection in API user secrets.
They use separate test databases, including pet_hotel_tests.
The database user needs permission to create and delete test databases.
Do not store real data in test databases.

Visual Studio 2022

Install the ASP.NET and web development workload and the .NET 9 SDK.
Open PetHotelManagement.sln and build the solution.
Set PetHotel.Api as the startup project and select the http launch profile.
Run the frontend in a terminal using the commands above.
Run the NUnit tests from Test Explorer.

Common problems

If the page reports a connection error, check that the API is running on port 5041.
If database login fails, check your PostgreSQL service, username and password.
If tables or columns are missing, run the database update command again.
If no rooms are shown, restart the backend and check the booking dates.
If a build says a file is in use, stop the running API with Ctrl+C and build again.
After changing backend code, restart the API.
If frontend packages change, run npm ci again.
