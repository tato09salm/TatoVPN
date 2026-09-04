cd TatoVPN_Panel && npm run build

cd C:\Users\LENOVO\Downloads\TatoVPN\TatoVPN_Panel
npm run dev

1- C:\Users\jeanm\Downloads\TatoVPN\TatoVPN_Panel ejecuta:
npm install
2- crear bd en postgres y asiganr las variables en .env
3- C:\Users\jeanm\Downloads\TatoVPN\TatoVPN_Panel ejecuta:
npm run prisma:push -> Esto crea las tablas en la base de datos
npm run prisma:seed -> Esto llena la base de datos con datos de prueba
npm run dev -> Esto levanta el panel de administración en http://localhost:3000 (modo desarrollo)
4- Credenciales:

- Email: admin@tatovpn.com
- Password: admin123
  5- Para modo produccion:
  npm run build
  npm run start
