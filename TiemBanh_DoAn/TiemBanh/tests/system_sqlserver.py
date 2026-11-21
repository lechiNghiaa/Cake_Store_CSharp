#!/usr/bin/env python3
"""Kiểm thử hệ thống SQL Server thật sau khi Compose chạy. Tạo dữ liệu TEST*, không xóa dữ liệu có sẵn."""
import json, os, pathlib, urllib.request, urllib.error, uuid, concurrent.futures
BASE=os.environ.get('TIEMBANH_API_URL','http://localhost:8081').rstrip('/')
root=pathlib.Path(__file__).resolve().parents[1]
cfg=dict(line.split('=',1) for line in (root/'.env').read_text().splitlines() if line and not line.startswith('#'))
password=cfg['ADMIN_PASSWORD']
def call(path,method='GET',body=None,token=None):
 data=json.dumps(body).encode() if body is not None else None
 req=urllib.request.Request(BASE+'/api'+path,data=data,method=method,headers={'Content-Type':'application/json',**({'Authorization':'Bearer '+token} if token else {})})
 try:
  with urllib.request.urlopen(req,timeout=45) as r:return r.status,json.loads(r.read() or b'{}')
 except urllib.error.HTTPError as e:return e.code,json.loads(e.read() or b'{}')
def ok(path,method='GET',body=None,token=None):
 status,data=call(path,method,body,token)
 assert 200<=status<300,(path,status,data)
 return data
# Đăng nhập — dùng tài khoản demo, không in token hoặc mật khẩu.
tokens={name:ok('/auth/login','POST',{'userName':name,'password':password})['token'] for name in ['admin','nhanvien','giaohang','khachhang']}
a,e,d,c=(tokens[n] for n in ['admin','nhanvien','giaohang','khachhang'])
# Quản lý bánh và tồn kho — dữ liệu riêng cho mỗi lượt kiểm thử.
code='TEST'+uuid.uuid4().hex[:10].upper()
cat=ok('/cakes/categories')[0]['id']
cake=ok('/cakes','POST',{'code':code,'name':'Bánh kiểm thử SQL '+code,'categoryId':cat,'price':50000},a)
cakeid=cake['id']
def stock():return next(x for x in ok('/cakes/manage?search='+code,token=a)['items'] if x['id']==cakeid)
ok(f'/cakes/{cakeid}/stock','PUT',{'onHand':10,'version':stock()['stockVersion'],'reason':'System test'},a)
def request(qty):return {'requestId':str(uuid.uuid4()),'lines':[{'cakeId':cakeid,'quantity':qty}],'recipient':'TEST','phone':'0900000000','address':'Địa chỉ kiểm thử'}
# Cạnh tranh tồn — lượng 7+7 vượt khả dụng 10; không bán vượt.
payloads=[request(7),request(7)]
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:results=list(pool.map(lambda p:call('/orders','POST',p,c),payloads))
assert sum(200<=x[0]<300 for x in results)==1,results
assert stock()['reserved']==7
order=next(x[1] for x in results if 200<=x[0]<300)
ok(f"/orders/{order['id']}/actions",'POST',{'action':'cancel','version':order['version']},c)
assert stock()['reserved']==0
# Đặt COD / Chống gửi lặp / Giao hàng / Đối soát.
payload=request(2);o=ok('/orders','POST',payload,c)
assert ok('/orders','POST',payload,c)['id']==o['id']
assert o['total']==120000
for action in ['confirm','prepare']:
 o=ok(f"/orders/{o['id']}/actions",'POST',{'action':action,'version':o['version']},e)
driver=next(x for x in ok('/management/drivers',token=e) if x['userName']=='giaohang')
o=ok(f"/orders/{o['id']}/actions",'POST',{'action':'assign','version':o['version'],'driverId':driver['id']},e)
# Người giao được seed đầu tiên là giaohang trong database mới.
assert call(f"/orders/{o['id']}/actions",'POST',{'action':'complete','version':o['version'],'deliveryAttemptId':o['deliveries'][-1]['id'],'collectedAmount':119000},d)[0]==409
o=ok(f"/orders/{o['id']}/actions",'POST',{'action':'complete','version':o['version'],'deliveryAttemptId':o['deliveries'][-1]['id'],'collectedAmount':120000},d)
assert o['status']=='Completed' and stock()['onHand']==8 and stock()['reserved']==0
receipt=next(x for x in ok('/management/receipts',token=e) if x['orderId']==o['id'])
p=ok('/management/periods','POST',{'items':[{'receiptId':receipt['id'],'received':receipt['amount']}]},e)
for action in ['approve','lock']:p=ok(f"/management/periods/{p['id']}/{action}",'POST',{'version':p['version']},a)
assert p['status']=='Locked'
print('PASS: SQL race/stock, idempotency, order/COD and handover. Cake test:',code)
