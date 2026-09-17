import 'package:flutter/material.dart';
import '../../core/services/whatsapp_service.dart';
import '../../widgets/app_ui.dart';

class ConfiguracionWhatsAppScreen extends StatefulWidget {
  const ConfiguracionWhatsAppScreen({super.key});
  @override State<ConfiguracionWhatsAppScreen> createState() => _State();
}
class _State extends State<ConfiguracionWhatsAppScreen> {
  final service = WhatsAppService();
  final nombre = TextEditingController(), enlace = TextEditingController(), texto = TextEditingController();
  bool loading = true, saving = false;
  String? error;
  @override void initState() { super.initState(); _load(); }
  @override void dispose() { nombre.dispose(); enlace.dispose(); texto.dispose(); super.dispose(); }
  Future<void> _load() async { try { final c = await service.getConfiguracion(); if (!mounted) return;
    nombre.text=c.nombreComercial; enlace.text=c.whatsappGroupInviteUrl??''; texto.text=c.textoInicialChat??'';
  } catch(e) { error=e.toString(); } finally { if(mounted) setState(()=>loading=false); } }
  Future<void> _save() async { setState(()=>saving=true); try {
    await service.guardarConfiguracion(ConfiguracionWhatsApp(nombreComercial:nombre.text.trim(),
      whatsappGroupInviteUrl: enlace.text.trim().isEmpty?null:enlace.text.trim(), textoInicialChat:texto.text.trim().isEmpty?null:texto.text.trim()));
    if(mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content:Text('Configuración guardada')));
  } catch(e) { if(mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content:Text(e.toString()))); }
  finally { if(mounted)setState(()=>saving=false); } }
  @override Widget build(BuildContext context) => Scaffold(appBar:AppBar(title:const Text('Configuración de WhatsApp')),
    body: loading?const Center(child:CircularProgressIndicator()):AppPage(child:Column(crossAxisAlignment:CrossAxisAlignment.start,children:[
      const SectionHeader(icon:Icons.settings_outlined,title:'WhatsApp del gimnasio',subtitle:'Configuración única para el gimnasio actual'),
      if(error!=null) Padding(padding:const EdgeInsets.symmetric(vertical:16),child:Text(error!)),
      const SizedBox(height:20), TextField(controller:nombre,decoration:const InputDecoration(labelText:'Nombre comercial')),
      const SizedBox(height:14), TextField(controller:enlace,decoration:const InputDecoration(labelText:'Enlace de invitación al grupo',hintText:'https://chat.whatsapp.com/...')),
      const SizedBox(height:14), TextField(controller:texto,maxLength:200,decoration:const InputDecoration(labelText:'Texto inicial para abrir chat individual')),
      const SizedBox(height:20), FilledButton.icon(onPressed:saving?null:_save,icon:const Icon(Icons.save_outlined),label:Text(saving?'Guardando...':'Guardar')),
    ])));
}
