
int userInput = 0;
do
{
    userInput = DisplayMenu();
    switch (userInput)
    {
        case 1:
            // call functional library code here;
            break;
        case 2:
            // call functional...
            break;
        default:
            break;
    }
} while (userInput != 5);


static int DisplayMenu()
{
    Console.WriteLine("Sorting");
    Console.WriteLine();
    Console.WriteLine("1. ");
    Console.WriteLine("2. ");
    Console.WriteLine("3. ");
    Console.WriteLine("4. ");
    Console.WriteLine("5. Exit");
    var result = Console.ReadLine();
    return Convert.ToInt32(result);
}