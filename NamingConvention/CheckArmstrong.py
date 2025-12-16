def calculate_armstrong_sum(number):
    armstrong_sum = 0
    digit_count = 0

    temp_number = number
    while temp_number > 0:
        digit_count += 1
        temp_number //= 10

    temp_number = number
    for _ in range(1, temp_number + 1):
        digit = temp_number % 10
        armstrong_sum += (digit ** digit_count)
        temp_number //= 10

    return armstrong_sum

user_number = int(input("\nPlease Enter the Number to Check for Armstrong: "))

if user_number == calculate_armstrong_sum(user_number):
    print(f"\n {user_number} is an Armstrong Number.\n")
else:
    print(f"\n {user_number} is Not an Armstrong Number.\n")