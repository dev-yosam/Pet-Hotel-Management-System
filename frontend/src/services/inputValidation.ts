const personName = /^(?=.*\p{L})[\p{L}\p{M} .'’-]+$/u
const petName = /^(?=.*\p{L})[\p{L}\p{M}0-9 .'’-]+$/u
const animalType = /^(?=.*\p{L})[\p{L}\p{M} .'’/-]+$/u
const phone = /^(?=(?:[^0-9]*[0-9]){7,15}[^0-9]*$)\+?(?:[0-9]+|\([0-9]+\))(?:[ -]?(?:[0-9]+|\([0-9]+\)))*$/
const email = /^(?=[^@]{1,64}@)[A-Za-z0-9_%+'-]+(?:\.[A-Za-z0-9_%+'-]+)*@(?:[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,63}$/

export function validateOwner(name: string, number: string, address: string) {
  return {
    fullName: !name || name.length > 100 || !personName.test(name)
      ? 'Use 1 to 100 characters: letters, spaces, periods, apostrophes or hyphens.' : '',
    phone: number.length > 30 || !phone.test(number)
      ? 'Use 7 to 15 digits, a leading +, single separators and matching parentheses. Maximum 30 characters.' : '',
    email: address && (address.length > 254 || !email.test(address))
      ? 'Enter an email such as name@example.com, without spaces. Maximum 254 characters.' : '',
  }
}

export function validatePet(name: string, species: string, breed: string, notes: string) {
  return {
    name: !name || name.length > 100 || !petName.test(name)
      ? 'Use 1 to 100 characters including a letter. Digits, spaces, periods, apostrophes and hyphens are allowed.' : '',
    species: !species || species.length > 50 || !animalType.test(species)
      ? 'Use 1 to 50 characters: letters, spaces, periods, apostrophes, slashes or hyphens.' : '',
    breed: breed && (breed.length > 100 || !animalType.test(breed))
      ? 'Use up to 100 characters: letters, spaces, periods, apostrophes, slashes or hyphens.' : '',
    careInstructions: notes.length > 1000 ? 'Use no more than 1000 characters.' : '',
  }
}
