import random

def is_valid_guess(user_input):
    if user_input.isdigit() and 1 <= int(user_input) <= 100:
        return True
    else:
        return False


def main():
    secret_number = random.randint(1, 100)
    has_guessed_correctly = False
    guess_count = 0

    user_guess = input("Guess a number between 1 and 100: ")

    while not has_guessed_correctly:
        if not is_valid_guess(user_guess):
            user_guess = input("I won't count this one. Please enter a number between 1 and 100: ")
            continue
        else:
            guess_count += 1
            user_guess = int(user_guess)

        if user_guess < secret_number:
            user_guess = input("Too low. Guess again: ")
        elif user_guess > secret_number:
            user_guess = input("Too high. Guess again: ")
        else:
            print("You guessed it in", guess_count, "guesses!")
            has_guessed_correctly = True


main()
