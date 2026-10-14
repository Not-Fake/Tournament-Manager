using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Transactions;
using System.Xml.Linq;

[JsonPolymorphic(TypeDiscriminatorPropertyName = $"type")]
[JsonDerivedType(typeof(SoloPlayer), "solo")]
[JsonDerivedType(typeof(Team), "team")]

abstract class Participant
{
    public string Name {  get; set; }
    public string Country { get; set; }
    private double _score;
    public double Score
    {
        get { return _score; } 
        set 
        { 
            _score = value < 0 ? 0 : value;
        }
    }
    public Participant() { }
    public Participant(string name, string country, double score)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be just spaces");
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be just spaces");

        Name = name;
        Country = country;
        Score = score;
    }
    public abstract void CalculatePoints(int position);
}

class SoloPlayer : Participant
{
    public SoloPlayer() { }
    public SoloPlayer(string name, string country, double score) : base(name, country, score)
    {
        
    }
   
    public override void CalculatePoints(int position)
    {
        if (position <= 0)
            throw new ArgumentOutOfRangeException(nameof(position), "The position must be greather than zero.");

        if (position == 1)
        {
            Score += 100;
        }
        else if (position == 2)
        {
            Score += 60;
        }
        else if (position == 3)
        {
            Score += 40;
        }
        else
        {
            Score += 10;
        }
    }
}
class Team : Participant, ISponsorable
{
    private int _members;
    public int Members
    {
        get { return  _members; }
        set
        {
            _members = value < 0 ? 0 : (value > 6 ? 6 : value);
        }
    }
    public string Sponsor { get; set;  }

    public Team() { }
    public Team(string name, string country, double score, string sponsor, int members) : base(name, country, score)
    {
        if (string.IsNullOrWhiteSpace(sponsor))
            throw new ArgumentException("Sponsor cannot be just spaces.");
        Members = members;
        Sponsor = sponsor;
    }
    public override void CalculatePoints(int position)
    {
        if (position <= 0)
            throw new ArgumentOutOfRangeException(nameof(position), "The position must be greather than zero.");

        if (position == 1)
        {
            Score += 100 * Members / 2.0;
        }
        else if (position == 2)
        {
            Score += 60 * Members / 2.0;
        }
        else if (position == 3)
        {
            Score += 40 * Members / 2.0;
        }
        else
        {
            Score += 10 * Members / 2.0;
        }
    }
    public void AddSponsor(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be just spaces.");

        Sponsor = name;
    }
    public double GetSponsorValue()
    {
        return Score * 1.5;
    }
}

interface ISponsorable
{
    string Sponsor {  get; }

    void AddSponsor(string name);
    double GetSponsorValue();
}

class Tournament
{
    public string Name { get; set; }
    List<Participant> participants = new List<Participant>();

    public Tournament() { }
    public Tournament(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be just spaces");
        Name = name;
    }
    public void Register(Participant p)
    {
        if (p == null)
            throw new ArgumentNullException("The participant cannot be null");

        participants.Add(p);
    }
    public void AwardPoints(string name, int position)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The name cannot be just spaces or null");

        if (position <= 0)
            throw new ArgumentOutOfRangeException(nameof(position), "Position must be greater than 0");

        Participant foundParticipant = participants.FirstOrDefault(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (foundParticipant == null)
            throw new InvalidOperationException($"Participant '{name}' not found");

        foundParticipant.CalculatePoints(position);   
    }
    public void GetLeaderboard()
    {
        var leaderboard = participants.OrderByDescending(o => o.Score);

        foreach (var participant in leaderboard)
        {
            Console.WriteLine($"{participant.Name}: {participant.Score} Score.");
        }
    }
    public void GetTeamStats()
    {
        var isTeam = participants.OfType<Team>();

        foreach (var t in isTeam)
        {
            Console.WriteLine($"{t.Name} | {t.Sponsor}: {t.GetSponsorValue()}");
        }
    }
    public void AddSponsorToTeam(string teamName, string sponsorName)
    {
        Team? team = participants.OfType<Team>().FirstOrDefault(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase));

        if (team == null)
            throw new InvalidOperationException($"Team '{teamName}' not found.");
        team.AddSponsor(sponsorName);
    }
    public void Save(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or emty");

        
        string content = JsonSerializer.Serialize(this);
        File.WriteAllText(fileName, content);
    }
    public static Tournament Load(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or emty");

        if (!File.Exists(fileName))
            throw new FileNotFoundException("The file does not exits", fileName);

        
        string content = File.ReadAllText(fileName);
        Tournament? tournament = JsonSerializer.Deserialize<Tournament>(content);

        if (tournament == null)
            throw new InvalidOperationException("Could not load the tournament.");

        return tournament;
    }
}

class Program
{
    static void Main()
    {
        static string Capitalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            text = text.Trim();
            return char.ToUpper(text[0]) + text.Substring(1).ToLower();
        }
        Tournament tournament;
        string fileName = "tournament.json";
        if (File.Exists(fileName))
        {
            tournament = Tournament.Load(fileName);

            Console.WriteLine($"Tournamnet loaded: {tournament.Name}");            
        }
        else
        {
            Console.Write("Enter tournament name: ");
            string tName = Console.ReadLine();
            tournament = new Tournament(tName);
            Console.WriteLine($"New tournament created: {tournament.Name}");
        }
        bool runnig = true;
        while (runnig)
        {
            try
            {
                Console.WriteLine("\n=== TOURNAMENT MANAGER ===");
                Console.WriteLine("1. Register individual player");
                Console.WriteLine("2. Register Team");
                Console.WriteLine("3. Award points");
                Console.WriteLine("4. Show team stats");
                Console.WriteLine("5. Show leaderboard");
                Console.WriteLine("6. Add sponsor");
                Console.WriteLine("7. Save and exits");
                Console.Write("\nChoose: ");

                if (!int.TryParse(Console.ReadLine(), out int option))
                    throw new InvalidOperationException("The Choose must be just numbers.");

                switch (option)
                {
                    case 1:
                        Console.Write("Enter the name: ");
                        string name = Capitalize(Console.ReadLine());

                        Console.Write("Enter the country: ");
                        string country = Capitalize(Console.ReadLine());

                        SoloPlayer newSoloPlayer = new SoloPlayer(name, country, 0);
                        tournament.Register(newSoloPlayer);

                        Console.WriteLine("Individual player registered successfully.");
                        break;
                    case 2:
                        Console.Write("Enter the sponsor: ");
                        string s = Console.ReadLine();

                        Console.Write("Enter the country: ");
                        string c = Console.ReadLine();

                        Console.Write("Enter the name of Team: ");
                        string n = Console.ReadLine();

                        Console.Write("Enter number of members: ");
                        if (!int.TryParse(Console.ReadLine(), out int m))
                            throw new ArgumentException("Members must be a number.");

                        Team newTeam = new Team(n, c, 0, s, m);
                        tournament.Register(newTeam);
                        break;
                    case 3:

                        Console.Write("Enter a name: ");
                        string fN = Console.ReadLine();

                        Console.Write("Enter a position: ");
                        if (!int.TryParse(Console.ReadLine(), out int p))
                            throw new ArgumentException("Members must be a number.");

                        tournament.AwardPoints(fN, p);
                        break;
                    case 4:
                        tournament.GetLeaderboard();
                        break;
                    case 5:
                        tournament.GetTeamStats();
                        break;
                    case 6:
                        Console.Write("Enter the name: ");
                        string nameTeam = Capitalize(Console.ReadLine());

                        Console.Write("Enter the sponsor: ");
                        string sponsorTeam = Capitalize(Console.ReadLine());

                        tournament.AddSponsorToTeam(nameTeam, sponsorTeam);
                        Console.WriteLine("Sponsor updated successfully.");
                        break;
                    case 7:
                        tournament.Save(fileName);
                        Console.WriteLine("Tornament saved successfully.");
                        runnig = false;
                        break;
                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

    }
}