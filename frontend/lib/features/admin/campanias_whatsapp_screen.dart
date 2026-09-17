import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../core/services/alumnos_service.dart';
import '../../core/services/sucursales_service.dart';
import '../../core/services/whatsapp_service.dart';
import '../../models/alumno_model.dart';
import '../../models/sucursal_model.dart';
import '../../widgets/app_ui.dart';

class CampaniasWhatsAppScreen extends StatefulWidget { const CampaniasWhatsAppScreen({super.key}); @override State<CampaniasWhatsAppScreen> createState()=>_State(); }
class _State extends State<CampaniasWhatsAppScreen> {
  final service=WhatsAppService(); bool loading=true; String? error; List<CampaniaWhatsAppModel> items=[];
  static const estados=['Borrador','Encolando','En proceso','Finalizada','Parcial','Cancelada'];
  @override void initState(){super.initState();_load();}
  Future<void> _load() async { setState((){loading=true;error=null;}); try { final v=await service.getCampanias(); if(mounted)setState(()=>items=v); }
    catch(e){if(mounted)setState(()=>error=e.toString());} finally{if(mounted)setState(()=>loading=false);} }
  Future<void> _new() async { final draft=await showDialog<_Draft>(context:context,builder:(_)=>const _CampaignDialog()); if(draft==null||!mounted)return;
    try { final id=await service.crearCampania(nombre:draft.nombre,plantilla:draft.plantilla,variables:draft.variables,audiencia:draft.audiencia,sucursalId:draft.sucursalId,alumnoIds:draft.alumnoIds);
      final p=await service.preview(id); if(!mounted)return; final ok=await showDialog<bool>(context:context,builder:(c)=>AlertDialog(title:const Text('Confirmar campaña'),content:SizedBox(width:500,child:Column(mainAxisSize:MainAxisSize.min,crossAxisAlignment:CrossAxisAlignment.start,children:[
        Text('Plantilla: ${p.plantilla}'),Text('Audiencia: ${p.audiencia}'),const SizedBox(height:8),Text(p.contenido),const SizedBox(height:12),
        Text('${p.destinatarios} destinatarios · ${p.omitidos} omitidos'),...p.motivos.map((m)=>Text('• ${m['motivo']}: ${m['cantidad']}')),
        const SizedBox(height:12),const Text('Este envío puede generar cargos de Twilio. Confirmá la cantidad antes de continuar.',style:TextStyle(fontWeight:FontWeight.bold))])),actions:[TextButton(onPressed:()=>Navigator.pop(c,false),child:const Text('Cancelar')),FilledButton(onPressed:()=>Navigator.pop(c,true),child:Text('Enviar a ${p.destinatarios}'))]));
      if(ok==true) await service.confirmar(id,p); await _load();
    } catch(e){if(mounted)ScaffoldMessenger.of(context).showSnackBar(SnackBar(content:Text(e.toString())));} }
  @override Widget build(BuildContext context)=>Scaffold(appBar:AppBar(title:const Text('Campañas de WhatsApp'),actions:[IconButton(onPressed:loading?null:_load,icon:const Icon(Icons.refresh))]),floatingActionButton:FloatingActionButton.extended(onPressed:_new,icon:const Icon(Icons.add),label:const Text('Nueva campaña')),
    body:loading?const Center(child:CircularProgressIndicator()):error!=null?Center(child:Text(error!)):AppPage(child:Column(crossAxisAlignment:CrossAxisAlignment.start,children:[
      const SectionHeader(icon:Icons.campaign_outlined,title:'Campañas de WhatsApp',subtitle:'Promociones y avisos encolados mediante plantillas aprobadas'),const SizedBox(height:20),
      if(items.isEmpty)const EmptyState(icon:Icons.mark_chat_unread_outlined,title:'No hay campañas',message:'Creá un borrador para previsualizar su audiencia.')
      else ...items.map((x)=>Padding(padding:const EdgeInsets.only(bottom:12),child:SurfaceCard(child:Column(crossAxisAlignment:CrossAxisAlignment.start,children:[
        Row(children:[Expanded(child:Text(x.nombre,style:Theme.of(context).textTheme.titleMedium)),Text(estados[x.estado])]),
        Text('Creada por ${x.creador} · ${DateFormat('dd/MM/yyyy HH:mm').format(x.fechaCreacion.toLocal())}'),const SizedBox(height:10),
        Text('Total ${x.total} · Aceptadas por Twilio ${x.aceptadas} · Pendientes ${x.pendientes} · Fallidas ${x.fallidas} · Omitidas ${x.omitidas}'),
        if(x.estado==1||x.estado==2) Align(alignment:Alignment.centerRight,child:TextButton(onPressed:()async{await service.cancelar(x.id);await _load();},child:const Text('Cancelar pendientes'))),
      ]))))
    ])));
}
class _Draft { final String nombre; final int plantilla,audiencia; final Map<String,String> variables; final String? sucursalId; final List<String> alumnoIds;
  const _Draft(this.nombre,this.plantilla,this.audiencia,this.variables,this.sucursalId,this.alumnoIds); }
class _CampaignDialog extends StatefulWidget { const _CampaignDialog(); @override State<_CampaignDialog> createState()=>_CampaignDialogState(); }
class _CampaignDialogState extends State<_CampaignDialog> {
  final nombre=TextEditingController(),titulo=TextEditingController(),detalle=TextEditingController(); int plantilla=0,audiencia=0; String? sucursalId;
  List<SucursalModel> sucursales=[]; List<AlumnoModel> alumnos=[]; final selected=<String>{}; bool loading=true;
  @override void initState(){super.initState();Future.wait([SucursalesService().fetchAll(),AlumnosService().fetchAll()]).then((v){if(mounted)setState((){sucursales=v[0] as List<SucursalModel>;alumnos=v[1] as List<AlumnoModel>;loading=false;});});}
  @override void dispose(){nombre.dispose();titulo.dispose();detalle.dispose();super.dispose();}
  void submit(){if(nombre.text.trim().isEmpty||titulo.text.trim().isEmpty||detalle.text.trim().isEmpty)return;
    if(audiencia==1&&sucursalId==null)return;if(audiencia==2&&selected.isEmpty)return;
    Navigator.pop(context,_Draft(nombre.text.trim(),plantilla,audiencia,{plantilla==0?'titulo':'asunto':titulo.text.trim(),'detalle':detalle.text.trim()},sucursalId,selected.toList()));}
  @override Widget build(BuildContext context)=>AlertDialog(title:const Text('Nueva campaña'),content:SizedBox(width:520,child:loading?const Center(child:CircularProgressIndicator()):SingleChildScrollView(child:Column(mainAxisSize:MainAxisSize.min,children:[
    TextField(controller:nombre,decoration:const InputDecoration(labelText:'Nombre interno')),const SizedBox(height:10),
    DropdownButtonFormField<int>(initialValue:plantilla,decoration:const InputDecoration(labelText:'Plantilla'),items:const [DropdownMenuItem(value:0,child:Text('Promoción')),DropdownMenuItem(value:1,child:Text('Aviso general'))],onChanged:(v)=>setState(()=>plantilla=v!)),const SizedBox(height:10),
    TextField(controller:titulo,decoration:InputDecoration(labelText:plantilla==0?'Título':'Asunto')),const SizedBox(height:10),TextField(controller:detalle,maxLength:300,decoration:const InputDecoration(labelText:'Detalle')),const SizedBox(height:10),
    DropdownButtonFormField<int>(initialValue:audiencia,decoration:const InputDecoration(labelText:'Audiencia'),items:const [DropdownMenuItem(value:0,child:Text('Todos con consentimiento')),DropdownMenuItem(value:1,child:Text('Sucursal principal')),DropdownMenuItem(value:2,child:Text('Selección manual'))],onChanged:(v)=>setState(()=>audiencia=v!)),
    if(audiencia==1)DropdownButtonFormField<String>(decoration:const InputDecoration(labelText:'Sucursal'),items:sucursales.map((s)=>DropdownMenuItem(value:s.id,child:Text(s.nombre))).toList(),onChanged:(v)=>sucursalId=v),
    if(audiencia==2)...alumnos.map((a)=>CheckboxListTile(value:selected.contains(a.id),title:Text(a.nombre),subtitle:Text(a.telefono),onChanged:(v)=>setState((){v==true?selected.add(a.id):selected.remove(a.id);}))),
  ]))),actions:[TextButton(onPressed:()=>Navigator.pop(context),child:const Text('Cancelar')),FilledButton(onPressed:submit,child:const Text('Previsualizar'))]);
}
