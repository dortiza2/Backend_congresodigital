# Configuración de Cloudinary para Congreso Digital

## Paso 1: Crear cuenta en Cloudinary

1. Ve a [https://cloudinary.com](https://cloudinary.com)
2. Regístrate con tu email o cuenta de Google
3. Verifica tu cuenta por email
4. Ingresa al dashboard de Cloudinary

## Paso 2: Obtener credenciales

En el dashboard de Cloudinary, encuentra:
- **Cloud Name** (nombre del cloud)
- **API Key** (clave de API)
- **API Secret** (secreto de API)

## Paso 3: Configurar variables de entorno

### Para desarrollo local (.env)
```bash
# Cloudinary Configuration
CLOUDINARY_CLOUD_NAME=tu_cloud_name
CLOUDINARY_API_KEY=tu_api_key
CLOUDINARY_API_SECRET=tu_api_secret
```

### Para producción (Render)
En el panel de Render, agrega estas variables de entorno:
- `CLOUDINARY_CLOUD_NAME`
- `CLOUDINARY_API_KEY`
- `CLOUDINARY_API_SECRET`

## Paso 4: Estructura de carpetas

Las imágenes se organizarán automáticamente en estas carpetas:
```
congreso_digital/
├── speakers/          # Fotos de ponentes
├── winners/           # Fotos de ganadores individuales
├── teams/             # Fotos de equipos ganadores
├── activities/        # Fotos de actividades generales
└── workshops/         # Fotos de talleres
```

## Paso 5: Uso de la API

### Subir una imagen
```bash
curl -X POST "https://tu-api.com/api/images/upload" \
  -H "Authorization: Bearer tu_token" \
  -F "file=@foto.jpg" \
  -F "folder=speakers" \
  -F "entityId=123"
```

### Obtener URL de imagen con transformaciones
```bash
curl "https://tu-api.com/api/images/url/abc123?width=500&height=300"
```

### Eliminar una imagen
```bash
curl -X DELETE "https://tu-api.com/api/images/delete/abc123" \
  -H "Authorization: Bearer tu_token"
```

## Paso 6: Transformaciones disponibles

El sistema genera automáticamente estas versiones de cada imagen:
- **original**: Imagen sin transformar
- **thumbnail**: 150x150px (recorte cuadrado)
- **medium**: 500x500px
- **large**: 800x800px
- **avatar**: 200x200px (recorte cuadrado)
- **banner**: 1200x400px (panorámico)

## Paso 7: Validaciones

El sistema valida automáticamente:
- ✅ Formatos permitidos: JPG, PNG, GIF, WebP
- ✅ Tamaño máximo: 5MB por archivo
- ✅ Dimensiones máximas: 1920x1080px
- ✅ Calidad automática optimizada

## Paso 8: Seguridad

- Las credenciales nunca se exponen en el frontend
- Solo usuarios autenticados pueden subir imágenes
- Las URLs generadas son seguras (HTTPS)
- Las imágenes se almacenan con acceso privado por defecto

## Paso 9: Monitoreo

Cloudinary proporciona:
- Estadísticas de uso del almacenamiento
- Métricas de transferencia
- Logs de actividad
- Alertas de límites

## Paso 10: Límites del plan gratuito

Plan gratuito incluye:
- 25GB de almacenamiento
- 25GB de transferencia mensual
- Transformaciones ilimitadas
- 1,000,000 de transformaciones mensuales

## Solución de problemas comunes

### Error: "Cloudinary is not configured"
Verifica que las variables de entorno estén correctamente configuradas.

### Error: "Tipo de archivo no permitido"
Asegúrate de que el archivo sea JPG, PNG, GIF o WebP.

### Error: "El archivo excede el tamaño máximo"
Reduce el tamaño del archivo o comprime la imagen.

### Las imágenes no se muestran
Verifica que el publicId sea correcto y que la imagen exista en Cloudinary.

## Soporte

Para problemas con Cloudinary:
- Documentación: https://cloudinary.com/documentation
- Soporte: support@cloudinary.com
- Dashboard: https://cloudinary.com/console