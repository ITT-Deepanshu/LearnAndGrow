import random

def roll_dice(number_of_sides):
    rolled_number = random.randint(1, number_of_sides)
    return rolled_number


def main():
    dice_sides = 6
    is_running = True

    while is_running:
        user_choice = input("Ready to roll? Enter Q to Quit: ")

        if user_choice.lower() != "q":
            result = roll_dice(dice_sides)
            print("You have rolled a", result)
        else:
            is_running = False

main()