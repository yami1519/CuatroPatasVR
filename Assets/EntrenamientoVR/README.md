# EntrenamientoVR — modos, tableros y ritmo

Proyecto: `/Users/yamiurbano/Desktop/PRACTICAS_ING.MIGUEL/CuatroPatasVR`.

## Para jugar

Abrir `Assets/Scenes/EntrenamientoVR.unity` y pulsar Play. La escena espera a que el jugador elija una opcion; no genera objetivos automaticamente al entrar.

- **Practica:** cuatro niveles guiados, inicialmente 30 segundos cada uno y una cuenta regresiva de tres segundos por nivel. Incluye golpes, agacharse, esquivas laterales y combinaciones. Al completarla aparecen resultados y se ofrecen nuevamente Practica y Modo libre.
- **Modo libre:** se puede elegir directamente. Da cinco segundos para ponerse de pie, centrarse y mirar al frente. Durante ese tiempo no aparecen objetivos y el reloj de actividad no avanza. Despues empieza una partida sin limite de tiempo.

En visor, mirar la tarjeta deseada y pulsar el gatillo. La tarjeta enfocada cambia de tono. Tambien admite contacto de un controlador con seguimiento. En el editor se pueden usar los botones de pantalla o P/F.

## Ritmo del modo libre

Los bloques empiezan a 2.6 m/s y aumentan hasta 3.6 m/s durante ocho segundos activos. Las barras y paneles alcanzan 3.2 m/s. Se mantienen los cinco segundos de preparacion antes de empezar.

El orden combina rafagas de dos a cuatro golpes con movimientos barajados: agacharse, izquierda y derecha. Las manos y los intervalos varian; pueden encadenarse varias rafagas. No se repite el antiguo ciclo fijo de cinco acciones.

Al alcanzar el ritmo completo, las llegadas de golpes se separan entre 0.42 y 0.66 segundos; si interviene un movimiento corporal, se deja al menos 0.8 segundos. Puede verse la siguiente señal mientras termina la anterior, con un maximo de tres acciones activas y un solo obstaculo de movimiento. La distancia de aparicion varia ligeramente y conserva aproximadamente un segundo de anticipacion como minimo.

Para ajustar el ritmo: salir de Play, seleccionar el objeto con **Training Session** en el Inspector de EntrenamientoVR. **Free Maximum Speed** controla bloques; **Free Obstacle Maximum Speed**, barras y paneles; **Free Minimum/Maximum Interval**, las pausas entre golpes; **Free Movement Clearance**, la separacion de movimientos; **Free Warmup Seconds**, la subida gradual. Guardar la escena despues de cambiar valores.

Al empezar hay tiempo de preparacion; durante la partida no vuelve el panel superior de instrucciones. Las barras no llevan palabras y los paneles laterales solo muestran una flecha. Se conservan las flechas de los bloques como señal de la mano que toca usar. La musica sigue en bucle.

## Tableros laterales

Como se entrena en el lugar, se muestran datos reales de la sesion, sin inventar distancia recorrida, calorias ni una clasificacion de otros jugadores.

- **Tiempo activo:** excluye el menu, la cuenta regresiva y las pausas por falta de seguimiento.
- **Golpes:** bloques acertados con mano y velocidad validas.
- **Aciertos:** porcentaje de bloques resueltos con acierto: golpes / (golpes + bloques perdidos). Se muestra -- antes de resolver el primer bloque. Una esquiva fallida no altera este porcentaje de golpes.
- **Esquivas:** barras y paneles evitados correctamente.
- **Racha actual:** golpes y esquivas correctos consecutivos; un bloque perdido o una esquiva fallida la reinicia.
- **Mejor racha:** mejor secuencia de la sesion actual; no es un record historico guardado.

Los datos se reinician al comenzar otra practica o partida libre. Los tableros estan a los lados del carril, no pegados a la cabeza.

## Controles de prueba sin visor

Hacer clic en Game para que reciba el teclado.

| Control | Accion |
|---|---|
| P | Empezar Practica. |
| F | Empezar Modo libre con cinco segundos de preparacion. |
| J / K | Simular golpe izquierdo / derecho. |
| Mantener Espacio | Simular agacharse. |
| Mantener A / D | Simular movimiento lateral. |
| 1–4 | Probar un nivel durante la practica. |
| R | Reiniciar la practica. |
| Escape | Terminar el libre; desde practica o resultados, volver a la eleccion de modo. |

En mandos Oculus Touch, mantener B o Y durante un segundo termina el modo libre. Los controles del editor son de simulacion: J/K no exigen alcanzar fisicamente el bloque. En visor se usa la posicion real de manos/cabeza y el movimiento del controlador.

## Deteccion

El golpe real exige mano correcta, seguimiento y una velocidad minima inicial de 0.25 m/s. Agacharse y esquivar usan umbrales iniciales de 30 cm respecto a la posicion calibrada de pie. Los obstaculos se evaluan al llegar al jugador, no cuando aparecen. La trayectoria no persigue al jugador.

La progresion de practica depende del tiempo, no exige un porcentaje minimo de aciertos. El libre no termina por errores ni aumenta su velocidad sin limite. No se implementa pago ni se modifica Wallet/SaveData.

## Archivos de esta actualizacion

- `Assets/Scenes/EntrenamientoVR.unity`: tarjetas de modo y tableros con referencias configuradas.
- `Assets/EntrenamientoVR/Scripts/TrainingSession.cs`: eleccion de modo, preparacion, intensidad, resultados y metricas.
- `Assets/EntrenamientoVR/Scripts/TrainingSpawner.cs`: secuencias aleatorias y separacion temporal de las llegadas.
- `Assets/EntrenamientoVR/Scripts/TrainingModeButton.cs`: seleccion de tarjetas con gatillo, enfoque o contacto.
- `Assets/EntrenamientoVR/Scripts/TrainingStatsBoard.cs`: tableros de tiempo, precision y rachas.
- `Assets/EntrenamientoVR/Presentacion/`: materiales de los paneles.

Se conservan el escenario, musica, base XR y los scripts de manos/objetivos/obstaculos. Menu, Casa y Correr y el proyecto original 4 Patas permanecen sin cambios.

## Validacion

Pruebas en Unity 6000.3.24f1: entrada sin generacion prematura, seleccion de practica, eleccion con mirada y gatillo sintetico, cinco segundos de preparacion sin reloj ni objetivos, dos bloques simultaneos, velocidad real superior a la anterior, mano correcta, metricas y porcentajes, barra sin texto, un solo obstaculo de movimiento activo, progresion de cuatro niveles hasta resultados, limites del flujo rapido y salida con boton de mando mantenido. Tambien se generaron capturas del menu, preparacion y tableros durante Play.

La prueba con visor fisico sigue pendiente. El proyecto mantiene su plataforma seleccionada Web; para desplegar OpenXR hay que elegir una plataforma compatible y completar la configuracion del dispositivo.

Commit propuesto: `Añade seleccion de modo, tableros y mayor ritmo al entrenamiento`.

### Prueba del ritmo revisado

Prueba automatizada durante Play con controles simulados, comparando el flujo anterior con el nuevo. Antes, las barras y paneles tardaban 2.35 segundos en llegar. En la muestra nueva tardaron entre 1.14 y 1.27 segundos al ritmo completo. Se generaron 24 acciones; las 22 ya resueltas registraron 17 golpes, cinco esquivas y cero fallos. Se comprobaron orden de llegadas, separacion corporal, limite de acciones activas, variedad de movimientos y acceso a practica. Esta simulacion comprueba la logica; la sensacion de juego con visor fisico requiere una prueba del usuario.
