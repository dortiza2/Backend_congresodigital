const bcrypt = require('bcryptjs');

const passwords = {
    'Test Student': 'Test123!',
    'Test Assistant': 'Test123!',
    'Test Admin': 'Test123!',
    'Test SuperAdmin': 'Test123!'
};

console.log('Password hashes:');
for (const [name, password] of Object.entries(passwords)) {
    const hash = bcrypt.hashSync(password, 10);
    console.log(`${name}: ${hash}`);
}