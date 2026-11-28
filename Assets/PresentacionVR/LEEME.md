# Presentación de Cuatro Patas VR

El fondo del menú representa el hogar del gato con objetos de entrenamiento VR.

## Editar los efectos desde Unity

En Assets/PresentacionVR/Animaciones están los clips y controladores:
- EntradaMenu: aparición mediante CanvasGroup.
- LogoRespira: movimiento suave vertical del logo.
- GatoRespira: respiración de la mascota.
- TarjetaReposo y TarjetaEnfocada: colores de las tarjetas del entrenamiento. El controlador SeleccionEntrenamiento usa el parámetro Focused.

Selecciona el objeto y abre Window > Animation > Animation para editar su clip. Los botones del menú usan la transición Color Tint de Unity y conservan el sonido de clic cuando estaba asignado.

Menu ya no usa los componentes MenuAnimator ni MenuButtonFX. Se conservan los scripts originales para no afectar otras escenas que pudieran utilizarlos. Se retiró del proyecto la herramienta temporal que creó estos recursos; no hace falta para reproducirlos ni editarlos.

El entrenamiento conserva código para las reglas, generación de objetivos, colisiones, seguimiento del visor, puntuación y movimiento dependiente de dificultad. El resaltado visual de tarjetas está en Animator.

Los botones Cuidar gato y Entrenamiento VR abren casaVR y EntrenamientoVR. No se modificó la escena casaVR.

Validación: compilación en Unity 6000.3.24f1 y comprobación de enlaces, controladores y clips completadas. El menú se comprobó visualmente en Play antes del último ajuste de espacio de sus rótulos. Pendiente la comprobación visual final y la prueba con visor físico.
